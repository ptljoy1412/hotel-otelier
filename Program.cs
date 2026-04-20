using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OtelierBackend.Authorization;
using OtelierBackend.Data;
using OtelierBackend.Options;
using OtelierBackend.Services;
using OtelierBackend.Services.Auth;
using OtelierBackend.Services.Bookings;
using Serilog;
using Serilog.Events;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddScoped<AppDbInitializer>();

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
    // Convert postgres:// URI to Npgsql format if needed (Render provides URI format)
    if (connStr.StartsWith("postgres://") || connStr.StartsWith("postgresql://"))
    {
        var uri = new Uri(connStr);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var sslMode = connStr.Contains("sslmode=disable") ? "Disable" : "Require";
        connStr = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={username};Password={password};SslMode={sslMode}";
    }
    options.UseNpgsql(connStr);
});

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>() ?? new JwtOptions();

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret));

// Do NOT clear the claim type map - let it map role claims correctly
// JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,

            IssuerSigningKey = signingKey,

            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("StaffOrReception", policy =>
        policy.RequireAssertion(ctx =>
        {
            var role = ctx.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            return role == ApplicationRoles.Staff || role == ApplicationRoles.Reception;
        }));
    options.AddPolicy("AnyRole", policy =>
        policy.RequireAssertion(ctx =>
        {
            var role = ctx.User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;
            return role == ApplicationRoles.Staff
                || role == ApplicationRoles.Reception
                || role == ApplicationRoles.Guest;
        }));
});

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Otelier Booking API",
        Version = "v1",
        Description = "Hotel booking API secured with local JWT bearer tokens."
    });

    const string bearerScheme = JwtBearerDefaults.AuthenticationScheme;

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste ONLY the token (without 'Bearer '). Swagger adds it automatically.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };

    options.AddSecurityDefinition(bearerScheme, securityScheme);
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference(bearerScheme, document, null), [] }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<AppDbInitializer>();
    await initializer.InitializeAsync();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();

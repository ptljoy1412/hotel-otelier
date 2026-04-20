using Microsoft.EntityFrameworkCore;
using Npgsql;
using OtelierBackend.Authorization;
using OtelierBackend.Models;
using OtelierBackend.Services.Auth;
using System.Data;
using System.Data.Common;

namespace OtelierBackend.Data;

public sealed class AppDbInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AppDbInitializer> _logger;

    public AppDbInitializer(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<AppDbInitializer> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDatabaseExistsAsync(cancellationToken);
        await EnsureLegacyInitialMigrationHistoryAsync(cancellationToken);
        await _dbContext.Database.MigrateAsync(cancellationToken);
        await SeedHotelsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
    }

    private async Task EnsureDatabaseExistsAsync(CancellationToken cancellationToken)
    {
        var connectionString = _dbContext.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }

        // Convert postgres:// URI to Npgsql key-value format if needed
        connectionString = ConvertToNpgsqlConnectionString(connectionString);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var targetDatabase = builder.Database;
        if (string.IsNullOrWhiteSpace(targetDatabase))
        {
            throw new InvalidOperationException("Target database name is not configured.");
        }

        var adminBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "postgres",
            Pooling = false
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @databaseName;";
        existsCommand.Parameters.AddWithValue("@databaseName", targetDatabase);

        var exists = await existsCommand.ExecuteScalarAsync(cancellationToken) is not null;
        if (exists)
        {
            return;
        }

        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE {QuoteIdentifier(targetDatabase)};";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Created database {DatabaseName}.", targetDatabase);
    }

    private async Task EnsureLegacyInitialMigrationHistoryAsync(CancellationToken cancellationToken)
    {
        const string initialMigrationId = "20260416145501_InitialCreate";
        const string productVersion = "10.0.6";

        var connection = _dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var hotelsExist = await TableExistsAsync(connection, "hotels", cancellationToken);
            var bookingsExist = await TableExistsAsync(connection, "bookings", cancellationToken);

            if (!hotelsExist || !bookingsExist)
            {
                return;
            }

            await _dbContext.Database.ExecuteSqlRawAsync(
                """
                CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                    "MigrationId" character varying(150) NOT NULL,
                    "ProductVersion" character varying(32) NOT NULL,
                    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
                );
                """);

            var migrationExists = await MigrationExistsAsync(connection, initialMigrationId, cancellationToken);
            if (migrationExists)
            {
                return;
            }

            await _dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ({0}, {1});
                """,
                initialMigrationId,
                productVersion);

            _logger.LogInformation("Registered the initial migration in EF history for an existing legacy schema.");
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task SeedHotelsAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Hotels.AnyAsync(cancellationToken))
        {
            return;
        }

        _dbContext.Hotels.AddRange(
            new Hotel { Id = 1, Name = "Otelier Downtown" },
            new Hotel { Id = 2, Name = "Otelier Airport" });

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded demo hotels.");
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        _dbContext.Users.AddRange(
            CreateUser("guest.user", "Guest@123", ApplicationRoles.Guest),
            CreateUser("staff.user", "Staff@123", ApplicationRoles.Staff),
            CreateUser("reception.user", "Reception@123", ApplicationRoles.Reception));

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded demo users for JWT authentication.");
    }

    private User CreateUser(string userName, string password, string role)
    {
        return new User
        {
            UserName = userName,
            NormalizedUserName = userName.Trim().ToUpperInvariant(),
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = role,
            IsActive = true
        };
    }

    private static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public'
                  AND lower(table_name) = @tableName
            );
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static async Task<bool> MigrationExistsAsync(
        DbConnection connection,
        string migrationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = @migrationId
            );
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@migrationId";
        parameter.Value = migrationId;
        command.Parameters.Add(parameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is true;
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    // Converts postgres://user:password@host:port/database?sslmode=require
    // to Npgsql key-value format: Host=...;Port=...;Database=...;Username=...;Password=...
    private static string ConvertToNpgsqlConnectionString(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://") && !connectionString.StartsWith("postgresql://"))
            return connectionString; // already in key-value format

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        var query = uri.Query.TrimStart('?');
        var sslMode = "Require";
        if (query.Contains("sslmode=disable", StringComparison.OrdinalIgnoreCase))
            sslMode = "Disable";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode={sslMode}";
    }
}

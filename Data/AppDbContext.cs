using Microsoft.EntityFrameworkCore;
using OtelierBackend.Models;

namespace OtelierBackend.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.Property(h => h.Id).ValueGeneratedOnAdd();
            entity.Property(h => h.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(b => b.BookingId).ValueGeneratedOnAdd();
            entity.Property(b => b.GuestName).HasMaxLength(120).IsRequired();
            entity.Property(b => b.CreatedBy).HasMaxLength(100).IsRequired();
            entity.HasIndex(b => new { b.HotelId, b.CheckInDate, b.CheckOutDate });
            entity.HasOne(b => b.Hotel)
                .WithMany(h => h.Bookings)
                .HasForeignKey(b => b.HotelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.Id).ValueGeneratedOnAdd();
            entity.Property(u => u.UserName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.NormalizedUserName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(30).IsRequired();
            entity.HasIndex(u => u.NormalizedUserName).IsUnique();
        });
    }
}

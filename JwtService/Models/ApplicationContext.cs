using Microsoft.EntityFrameworkCore;

namespace JwtService.Models;

public class ApplicationContext : DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> opts) : base(opts) {}
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshSession> RefreshSessions { get; set; } = null!;
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RefreshSession>()
            .HasOne(r => r.User)
            .WithMany(u => u.RefreshSessions)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RefreshSession>(r =>
        {
            r.Property(p => p.UA).HasMaxLength(200);
            r.Property(p => p.Ip).HasMaxLength(15);
            r.Property(p => p.Fingerprint).HasMaxLength(200);
        });
    }
}
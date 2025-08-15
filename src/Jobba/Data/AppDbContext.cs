using Jobba.Models;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<JobApplication>(e =>
        {
            e.Property(p => p.Company).HasMaxLength(200).IsRequired();
            e.Property(p => p.Role).HasMaxLength(200).IsRequired();
            e.Property(p => p.Source).HasMaxLength(200);
            e.Property(p => p.Location).HasMaxLength(200);
            e.Property(p => p.SalaryRange).HasMaxLength(100);
            e.Property(p => p.Status).HasConversion<int>();
            e.HasIndex(p => new { p.Company, p.Role });

            e.HasMany<Activity>()
             .WithOne(a => a.JobApplication!)
             .HasForeignKey(a => a.JobApplicationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Activity>(e =>
        {
            e.Property(a => a.Type).HasMaxLength(100).IsRequired();
            e.Property(a => a.Details).HasMaxLength(1000);
        });
    }
}

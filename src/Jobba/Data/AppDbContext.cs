using Jobba.Models;
using Microsoft.EntityFrameworkCore;

namespace Jobba.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JobApplication> Applications => Set<JobApplication>();

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
        });
    }
}

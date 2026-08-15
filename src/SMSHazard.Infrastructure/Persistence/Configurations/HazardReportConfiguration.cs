using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence.Configurations;

public class HazardReportConfiguration : IEntityTypeConfiguration<HazardReport>
{
    public void Configure(EntityTypeBuilder<HazardReport> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ReferenceNo).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.ReferenceNo).IsUnique();
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).IsRequired().HasMaxLength(4000);
        b.Property(x => x.ImmediateActionTaken).HasMaxLength(2000);
        b.Property(x => x.ReportedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // Anonymous public reporting: tracking code is optional but unique when present.
        // (On PostgreSQL, NULLs are distinct in a unique index, so authenticated reports do not collide.)
        b.Property(x => x.TrackingCode).HasMaxLength(20);
        b.HasIndex(x => x.TrackingCode).IsUnique();

        b.HasOne(x => x.HazardCategory).WithMany(c => c.Hazards)
            .HasForeignKey(x => x.HazardCategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Department).WithMany(d => d.Hazards)
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Assessments).WithOne(a => a.HazardReport)
            .HasForeignKey(a => a.HazardReportId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.CorrectiveActions).WithOne(a => a.HazardReport)
            .HasForeignKey(a => a.HazardReportId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Attachments).WithOne(a => a.HazardReport)
            .HasForeignKey(a => a.HazardReportId).OnDelete(DeleteBehavior.Cascade);
    }
}

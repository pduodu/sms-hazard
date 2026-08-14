using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence.Configurations;

public class CorrectiveActionConfiguration : IEntityTypeConfiguration<CorrectiveAction>
{
    public void Configure(EntityTypeBuilder<CorrectiveAction> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(2000);
        b.Property(x => x.AssignedToId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProgressNote).HasMaxLength(2000);
        b.Property(x => x.EffectivenessNote).HasMaxLength(2000);
        b.HasIndex(x => new { x.Status, x.DueDate });
    }
}

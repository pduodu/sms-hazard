using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence.Configurations;

public class RiskAssessmentConfiguration : IEntityTypeConfiguration<RiskAssessment>
{
    public void Configure(EntityTypeBuilder<RiskAssessment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Rationale).HasMaxLength(2000);
        b.Property(x => x.AssessedById).IsRequired().HasMaxLength(450);
        b.Property(x => x.RiskLevel).HasConversion<string>().HasMaxLength(20);
    }
}

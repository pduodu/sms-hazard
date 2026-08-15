using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.OrganizationName).IsRequired().HasMaxLength(200);
        b.Property(x => x.LogoPath).HasMaxLength(400);
        b.Property(x => x.SupportEmail).HasMaxLength(256);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SMSHazard.Domain.Entities;

namespace SMSHazard.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class HazardCategoryConfiguration : IEntityTypeConfiguration<HazardCategory>
{
    public void Configure(EntityTypeBuilder<HazardCategory> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).IsRequired().HasMaxLength(450);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        b.Property(x => x.LinkUrl).HasMaxLength(500);
        b.HasIndex(x => new { x.UserId, x.IsRead });
    }
}

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.FileName).IsRequired().HasMaxLength(260);
        b.Property(x => x.ContentType).IsRequired().HasMaxLength(150);
        b.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);
        b.Property(x => x.UploadedById).HasMaxLength(450);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.EntityName).IsRequired().HasMaxLength(150);
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.Property(x => x.Action).IsRequired().HasMaxLength(40);
        b.Property(x => x.ChangedById).HasMaxLength(450);
        b.Property(x => x.ChangeSummary).HasMaxLength(2000);
        b.HasIndex(x => new { x.EntityName, x.Timestamp });
    }
}

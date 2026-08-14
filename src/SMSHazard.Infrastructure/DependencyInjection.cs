using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SMSHazard.Application.Common;
using SMSHazard.Application.Interfaces;
using SMSHazard.Infrastructure.Email;
using SMSHazard.Infrastructure.Persistence;
using SMSHazard.Infrastructure.Services;
using SMSHazard.Infrastructure.Storage;

namespace SMSHazard.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure: EF Core (PostgreSQL), options binding, and adapters
    /// (email, storage, notifications). Identity is wired in the Web composition root.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? "Host=127.0.0.1;Port=5432;Database=smshazard;Username=smshazard;Password=CHANGE_ME;SSL Mode=Disable";

        services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

        services.Configure<EmailSettings>(config.GetSection("Email"));
        services.Configure<StorageSettings>(config.GetSection("Storage"));

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IAttachmentStorage, FileAttachmentStorage>();
        services.AddScoped<IHazardService, HazardService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<ICapaService, CapaService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IReminderService, ReminderService>();

        return services;
    }
}

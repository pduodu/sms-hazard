using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SMSHazard.Application.DTOs;
using SMSHazard.Application.Validators;

namespace SMSHazard.Application;

public static class DependencyInjection
{
    /// <summary>Registers Application-layer services (validators; use-case service interfaces are implemented in Infrastructure).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<CreateHazardRequest>, CreateHazardRequestValidator>();
        return services;
    }
}

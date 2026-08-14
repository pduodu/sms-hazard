using FluentValidation;
using SMSHazard.Application.DTOs;

namespace SMSHazard.Application.Validators;

/// <summary>Server-side validation rules for hazard creation (FluentValidation).</summary>
public sealed class CreateHazardRequestValidator : AbstractValidator<CreateHazardRequest>
{
    public CreateHazardRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.HazardCategoryId).GreaterThan(0).WithMessage("Please select a category.");
        RuleFor(x => x.DepartmentId).GreaterThan(0).WithMessage("Please select a department.");
        RuleFor(x => x.OccurrenceDate)
            .NotEmpty()
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1)).WithMessage("Occurrence date cannot be in the future.");
        RuleFor(x => x.ImmediateActionTaken).MaximumLength(2000);
    }
}

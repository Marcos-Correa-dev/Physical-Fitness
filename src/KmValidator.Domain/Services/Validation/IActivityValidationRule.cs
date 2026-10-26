using KmValidator.Domain.Entities;

namespace KmValidator.Domain.Services.Validation;

public interface IActivityValidationRule
{
    string RuleName { get; }
    int ExecutionOrder { get; }

    Task<ValidationRuleResult> ValidateAsync(PhysicalActivity activity, CancellationToken cancellationToken = default);
}

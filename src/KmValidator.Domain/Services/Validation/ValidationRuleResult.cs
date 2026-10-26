using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;

namespace KmValidator.Domain.Services.Validation;

public sealed record ValidationRuleResult
{
    public string RuleName { get; }
    public bool Passed { get; }
    public IssueSeverity Severity { get; }
    public string Message { get; }
    public string? Details { get; }
    public double? MeasuredValue { get; }

    private ValidationRuleResult(string ruleName, bool passed, IssueSeverity severity, string message, string? details = null, double? measuredValue = null)
    {
        RuleName = ruleName;
        Passed = passed;
        Severity = severity;
        Message = message;
        Details = details;
        MeasuredValue = measuredValue;
    }

    public static ValidationRuleResult Success(string ruleName, string message = "Validação aprovada sem inconformidades.", double? measuredValue = null) =>
        new(ruleName, true, IssueSeverity.Info, message, null, measuredValue);

    public static ValidationRuleResult Warning(string ruleName, string message, string? details = null, double? measuredValue = null) =>
        new(ruleName, false, IssueSeverity.Warning, message, details, measuredValue);

    public static ValidationRuleResult Failure(string ruleName, string message, string? details = null, double? measuredValue = null) =>
        new(ruleName, false, IssueSeverity.Critical, message, details, measuredValue);

    public ValidationIssue ToIssue() => new(RuleName, Severity, Message, Details);
}

using KmValidator.Domain.Enums;

namespace KmValidator.Domain.Entities;

public class ValidationIssue
{
    public long Id { get; private set; }
    public Guid ActivityValidationId { get; private set; }
    public string RuleName { get; private set; }
    public IssueSeverity Severity { get; private set; }
    public string Message { get; private set; }
    public string? Details { get; private set; }

    // Ef Core
    protected ValidationIssue()
    {
        RuleName = string.Empty;
        Message = string.Empty;
    }

    public ValidationIssue(string ruleName, IssueSeverity severity, string message, string? details = null)
    {
        RuleName = ruleName;
        Severity = severity;
        Message = message;
        Details = details;
    }

    internal void AttachValidation(Guid validationId)
    {
        ActivityValidationId = validationId;
    }
}

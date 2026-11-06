using KmValidator.Domain.Enums;
using KmValidator.Domain.ValueObjects;

namespace KmValidator.Domain.Entities;

public class ActivityValidation
{
    private readonly List<ValidationIssue> _issues = new();

    public Guid Id { get; private set; }
    public Guid PhysicalActivityId { get; private set; }
    public ValidationStatus Status { get; private set; }
    public double CalculatedGpsDistanceKm { get; private set; }
    public double DiscrepancyPercentage { get; private set; }
    public double MaxSpeedKmh { get; private set; }
    public double AveragePaceMinKm { get; private set; }
    public DateTime ValidatedAt { get; private set; }
    public string SummaryNotes { get; private set; }
    public IReadOnlyCollection<ValidationIssue> Issues => _issues.AsReadOnly();

    // Ef Core
    protected ActivityValidation()
    {
        SummaryNotes = string.Empty;
    }

    public ActivityValidation(
        Guid physicalActivityId,
        ValidationStatus status,
        double calculatedGpsDistanceKm,
        double discrepancyPercentage,
        double maxSpeedKmh,
        double averagePaceMinKm,
        string summaryNotes,
        IEnumerable<ValidationIssue>? issues = null)
    {
        Id = Guid.NewGuid();
        PhysicalActivityId = physicalActivityId;
        Status = status;
        CalculatedGpsDistanceKm = Math.Round(calculatedGpsDistanceKm, 3);
        DiscrepancyPercentage = Math.Round(discrepancyPercentage, 2);
        MaxSpeedKmh = Math.Round(maxSpeedKmh, 2);
        AveragePaceMinKm = Math.Round(averagePaceMinKm, 2);
        SummaryNotes = summaryNotes ?? string.Empty;
        ValidatedAt = DateTime.UtcNow;

        if (issues != null)
        {
            foreach (var issue in issues)
            {
                issue.AttachValidation(Id);
                _issues.Add(issue);
            }
        }
    }

    public void Update(
        ValidationStatus status,
        double calculatedGpsDistanceKm,
        double discrepancyPercentage,
        double maxSpeedKmh,
        double averagePaceMinKm,
        string summaryNotes,
        IEnumerable<ValidationIssue>? issues = null)
    {
        Status = status;
        CalculatedGpsDistanceKm = Math.Round(calculatedGpsDistanceKm, 3);
        DiscrepancyPercentage = Math.Round(discrepancyPercentage, 2);
        MaxSpeedKmh = Math.Round(maxSpeedKmh, 2);
        AveragePaceMinKm = Math.Round(averagePaceMinKm, 2);
        SummaryNotes = summaryNotes ?? string.Empty;
        ValidatedAt = DateTime.UtcNow;

        _issues.Clear();
        if (issues != null)
        {
            foreach (var issue in issues)
            {
                issue.AttachValidation(Id);
                _issues.Add(issue);
            }
        }
    }
}

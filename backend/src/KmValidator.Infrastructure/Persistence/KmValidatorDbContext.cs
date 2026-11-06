using KmValidator.Domain.Entities;
using KmValidator.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KmValidator.Infrastructure.Persistence;

public class KmValidatorDbContext : DbContext
{
    public KmValidatorDbContext(DbContextOptions<KmValidatorDbContext> options) : base(options) { }

    public DbSet<Athlete> Athletes => Set<Athlete>();
    public DbSet<WearableDevice> WearableDevices => Set<WearableDevice>();
    public DbSet<PhysicalActivity> PhysicalActivities => Set<PhysicalActivity>();
    public DbSet<RoutePoint> RoutePoints => Set<RoutePoint>();
    public DbSet<ActivityValidation> ActivityValidations => Set<ActivityValidation>();
    public DbSet<ValidationIssue> ValidationIssues => Set<ValidationIssue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Athlete Configuration
        modelBuilder.Entity<Athlete>(entity =>
        {
            entity.ToTable("athletes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.WeightKg).HasPrecision(5, 2);
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");
        });

        // 2. WearableDevice Configuration
        modelBuilder.Entity<WearableDevice>(entity =>
        {
            entity.ToTable("wearable_devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceName).HasMaxLength(120).IsRequired();
            entity.Property(e => e.Manufacturer).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SerialNumber).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.SerialNumber).IsUnique();
            entity.Property(e => e.DeviceType).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.RegisteredAt).HasColumnType("timestamp with time zone");
            entity.Property(e => e.LastSyncAt).HasColumnType("timestamp with time zone");

            entity.HasOne<Athlete>()
                .WithMany()
                .HasForeignKey(e => e.AthleteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 3. PhysicalActivity Configuration (Aggregate Root)
        modelBuilder.Entity<PhysicalActivity>(entity =>
        {
            entity.ToTable("physical_activities");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ActivityType).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.ReportedDistanceKm).HasPrecision(8, 3);
            entity.Property(e => e.ValidatedDistanceKm).HasPrecision(8, 3);
            entity.Property(e => e.StartTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.EndTime).HasColumnType("timestamp with time zone");
            entity.Property(e => e.CreatedAt).HasColumnType("timestamp with time zone");

            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Status);

            entity.HasOne<Athlete>()
                .WithMany()
                .HasForeignKey(e => e.AthleteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<WearableDevice>()
                .WithMany()
                .HasForeignKey(e => e.DeviceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.RoutePoints)
                .WithOne()
                .HasForeignKey(p => p.PhysicalActivityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Validation)
                .WithOne()
                .HasForeignKey<ActivityValidation>(v => v.PhysicalActivityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. RoutePoint Configuration
        modelBuilder.Entity<RoutePoint>(entity =>
        {
            entity.ToTable("route_points");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Latitude).HasPrecision(10, 6);
            entity.Property(e => e.Longitude).HasPrecision(10, 6);
            entity.Property(e => e.ElevationMeters).HasPrecision(7, 2);
            entity.Property(e => e.InstantSpeedKmh).HasPrecision(6, 2);
            entity.Property(e => e.Timestamp).HasColumnType("timestamp with time zone");

            entity.HasIndex(e => new { e.PhysicalActivityId, e.SequenceOrder });
        });

        // 5. ActivityValidation Configuration
        modelBuilder.Entity<ActivityValidation>(entity =>
        {
            entity.ToTable("activity_validations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.CalculatedGpsDistanceKm).HasPrecision(8, 3);
            entity.Property(e => e.DiscrepancyPercentage).HasPrecision(6, 2);
            entity.Property(e => e.MaxSpeedKmh).HasPrecision(6, 2);
            entity.Property(e => e.AveragePaceMinKm).HasPrecision(6, 2);
            entity.Property(e => e.SummaryNotes).HasMaxLength(500);
            entity.Property(e => e.ValidatedAt).HasColumnType("timestamp with time zone");

            entity.HasMany(e => e.Issues)
                .WithOne()
                .HasForeignKey(i => i.ActivityValidationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 6. ValidationIssue Configuration
        modelBuilder.Entity<ValidationIssue>(entity =>
        {
            entity.ToTable("validation_issues");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RuleName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Severity).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Message).HasMaxLength(350).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(500);

            entity.HasIndex(e => e.ActivityValidationId);
        });
    }
}

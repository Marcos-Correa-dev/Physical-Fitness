using System.Text;
using FluentAssertions;
using KmValidator.Application.Services;
using KmValidator.Domain.Enums;
using Xunit;

namespace KmValidator.UnitTests;

public class ActivityFileParserServiceTests
{
    [Fact]
    public void ParseTcx_ShouldParseMiFitnessStrengthTrainingTcxFileSuccessfully()
    {
        // XML real exportado pelo Mi Fitness para Musculação / Levantamento de peso
        string tcxContent = @"<?xml version='1.0' encoding='UTF-8' standalone='yes' ?>
<TrainingCenterDatabase creator=""Mi Fitness"" version=""1.0"" xmlns=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd"">
  <Activities>
    <Activity Sport="""">
      <Id>2026-10-17T11:58:41.000Z</Id>
      <Calories>537</Calories>
      <Lap>
        <TotalTimeSeconds>4689</TotalTimeSeconds>
        <Calories>393</Calories>
        <HeartRateBpm>135</HeartRateBpm>
      </Lap>
    </Activity>
  </Activities>
</TrainingCenterDatabase>";

        var parser = new ActivityFileParserService();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tcxContent));

        var telemetry = parser.ParseTcx(stream, "20261017Levantamento de peso.tcx", "Xiaomi Mi Band 10");

        telemetry.Should().NotBeNull();
        telemetry.ActivityType.Should().Be(ActivityType.StrengthTraining);
        telemetry.Title.Should().Be("Levantamento De Peso");
        telemetry.Manufacturer.Should().Be("Xiaomi");
        telemetry.ReportedDistanceKm.Should().Be(0.0);
        telemetry.CaloriesBurned.Should().Be(537);
        telemetry.AvgHeartRateBpm.Should().Be(135);
        telemetry.StartTimeUtc.Should().Be(DateTime.Parse("2026-10-17T11:58:41.000Z").ToUniversalTime());
        (telemetry.EndTimeUtc - telemetry.StartTimeUtc).TotalSeconds.Should().Be(4689);
        telemetry.GpsPoints.Should().BeEmpty();
    }
}

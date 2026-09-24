using FeBuddy.Core.Application.Airac.Airways;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Models;
using FeBuddy.Core.Domain.Airways.Models;
using FeBuddy.Core.Domain.Geo;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.Logging.Models;
using FeBuddy.Core.Infrastructure.Nasr.Models;

using NetTopologySuite.Geometries;

using FeBuddy.UnitTests.Application.Airac.Airways.Fixtures;

namespace FeBuddy.UnitTests.Application.Airac.Airways;

/// <summary>
/// Verifies the message levels from remediation plan 3.8: the buffer's "leg too short"
/// notice is Info (not Warning), airway exclusion and unrecognized-key notices are Warning,
/// and a run with only Info notices reports zero warnings and mirrors every message to
/// <see cref="AppLog"/>.
/// </summary>
[Collection("AppLog")]
public sealed class AirwayMessageLevelsTests : IDisposable
{
	private readonly string _output = Path.Combine(Path.GetTempPath(), "FeBuddyTests_Levels_" + Guid.NewGuid().ToString("N"));

	public AirwayMessageLevelsTests() => AppLog.ConfigureForTesting(Path.Combine(_output, "logs"));

	public void Dispose()
	{
		AppLog.ConfigureForTesting(null);
		try { if (Directory.Exists(_output)) Directory.Delete(_output, recursive: true); } catch { }
	}

	[Fact]
	public void a_leg_too_short_to_buffer_is_reported_as_info_not_warning()
	{
		AirwayPoint a = new("AAAAA", "WP", 40.00000, -80.00000, "fix");
		AirwayPoint b = new("BBBBB", "WP", 40.00100, -80.00100, "fix"); // ~0.08 NM away - well inside 2.5+2.5

		LineString leg = Wgs84.Factory.CreateLineString(new[]
		{
			new Coordinate(a.Longitude, a.Latitude),
			new Coordinate(b.Longitude, b.Latitude),
		});

		AirwayBufferResult result = AirwayWaypointBuffer.Buffer(new[] { leg }, new[] { a, b }, "TEST");

		ServiceMessage message = Assert.Single(result.Messages);
		Assert.Equal(LogLevel.Info, message.Level);
		Assert.Equal("AirwayWaypointBuffer", message.Source);
		Assert.Empty(result.Warnings); // the Info notice does not surface as a warning
	}

	[Fact]
	public void an_unrecognized_settings_key_is_a_warning_level_message()
	{
		Dictionary<string, string> settings = new()
		{
			{ "OutputDirectory", _output },
			{ "OutputBy", "HighLow" },
			{ "TotallyMadeUpKey", "x" },
		};

		AirwaySettingsParseResult result = AirwaySettingsParser.Parse(settings);

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("TotallyMadeUpKey"));
	}

	[Fact]
	public void airway_exclusion_is_a_warning_and_the_run_mirrors_messages_to_applog()
	{
		NasrCsvDataCollection data = AirwayTestDataBuilder.Build(
			fixes: new[] { ("AAAAA", 40.0, -80.0), ("CCCCC", 42.0, -82.0), ("DDDDD", 43.0, -83.0) },
			awyId: "J146",
			segments: new[]
			{
				AirwayTestDataBuilder.Segment("J146", 10, "AAAAA", "WP", "MISNG"),
				AirwayTestDataBuilder.Segment("J146", 20, "CCCCC", "WP", "DDDDD"),
			});

		AirwayServiceResult result = AirwayService.Run(data, new Dictionary<string, string>
		{
			{ "OutputDirectory", _output },
			{ "OutputBy", "None" },
			{ "GenerateAliasFile", "N" },
		});

		Assert.Contains(result.Messages, m => m.Level == LogLevel.Warning && m.Text.Contains("J146") && m.Text.Contains("excluded from all output"));

		// AirwayService.Run mirrors every message it collected to the shared application log.
		Assert.Contains(AppLog.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("J146"));
	}
}

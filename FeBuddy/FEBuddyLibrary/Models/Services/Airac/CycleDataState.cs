namespace FEBuddyLibrary.Models.Services.Airac;

/// <summary>
/// Where one AIRAC cycle sits in the launch data pipeline (probe -&gt; download -&gt; parse).
/// </summary>
/// <remarks>
/// <see cref="NotYetPublished"/> and <see cref="Failed"/> are different states and are shown
/// differently in the GUI: the first is normal and expected (the FAA has not released the
/// next cycle yet), the second is a real error.
/// </remarks>
public enum CycleDataState
{
	/// <summary>The FAA has not published this cycle's CSV data yet. Not an error.</summary>
	NotYetPublished = 0,

	/// <summary>Published, but not yet in the local cache.</summary>
	NotDownloaded = 1,

	/// <summary>The CSV archive is downloading and extracting.</summary>
	Downloading = 2,

	/// <summary>The CSV files are in the local cache, not yet parsed.</summary>
	Downloaded = 3,

	/// <summary>The CSV files are being parsed into a <c>NasrCsvDataCollection</c>.</summary>
	Parsing = 4,

	/// <summary>Parsed and available from the cache.</summary>
	Ready = 5,

	/// <summary>Download or parse failed after a retry. A real error.</summary>
	Failed = 6,
}

/// <summary>
/// The AIRAC Service's overall readiness, decided from the three cycles' <see cref="CycleDataState"/>.
/// </summary>
public enum AiracCycleReadiness
{
	/// <summary>At least one required cycle is still downloading, parsing, or not started.</summary>
	Waiting = 0,

	/// <summary>Every available cycle is parsed and ready; the service unlocks.</summary>
	Ready = 1,

	/// <summary>The service unlocks, but the previous and/or next cycle failed and is unselectable.</summary>
	Degraded = 2,

	/// <summary>The current cycle failed; the service stays disabled.</summary>
	Unavailable = 3,
}

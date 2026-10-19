using FeBuddy.Core.Domain.Airac.Models;
using FeBuddy.Core.Infrastructure.Dtpp;
using FeBuddy.Core.Infrastructure.Nasr.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// One cycle's slot in <see cref="AiracCycleDataCache"/>: its identity, current pipeline
/// state, the local folder its CSVs live in once downloaded, and the parse task the parsed
/// data can be awaited from.
/// </summary>
public sealed class AiracCycleDataCacheEntry
{
	internal AiracCycleDataCacheEntry(AiracCycleInfo cycle, AiracCyclePosition position)
	{
		Cycle = cycle;
		Position = position;
	}

	/// <summary>The cycle's identity (ID, effective date, CSV date string).</summary>
	public AiracCycleInfo Cycle { get; }

	/// <summary>Where this cycle sits relative to today.</summary>
	public AiracCyclePosition Position { get; }

	/// <summary>The cycle's current pipeline state.</summary>
	public CycleDataState State { get; internal set; } = CycleDataState.NotDownloaded;

	/// <summary>The local folder holding this cycle's CSV files, once downloaded.</summary>
	public string? CycleDirectory { get; internal set; }

	/// <summary>
	/// The parse task, queued, running or done; <see cref="AiracCycleDataCache.GetAsync"/> awaits it
	/// rather than starting a second parse.
	/// </summary>
	internal Task<NasrCsvDataCollection>? ParseTask { get; set; }

	/// <summary>
	/// The download (CSVs, then the d-TPP Metafile) under way or last run, shared by the launch
	/// pipeline and anything that needs the cycle on disk sooner, so a cycle is never downloaded
	/// twice at once.
	/// </summary>
	internal Task? DownloadTask { get; set; }

	/// <summary>
	/// The cycle's <see cref="DtppFiles.FileName"/> as the cache last found it: looked for once the
	/// cycle's folder is known, and again once the download has fetched it; <see langword="null"/> when
	/// the folder doesn't have it. <see cref="AiracCycleDataCache.FindDtppFile"/> answers from it.
	/// </summary>
	internal string? DtppFile { get; set; }

	/// <summary>
	/// Whether the FAA has been asked if it has published this cycle. A dev launch that leaves out
	/// downloading asks nobody, so a download asked for later asks first.
	/// </summary>
	internal bool Probed { get; set; }
}

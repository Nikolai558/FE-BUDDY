using FeBuddy.Core.Application.Airac.Airports.Models;
using FeBuddy.Core.Application.Airac.Airways.Models;
using FeBuddy.Core.Application.Airac.Arrivals.Models;
using FeBuddy.Core.Application.Airac.ArtccBoundaries.Models;
using FeBuddy.Core.Application.Airac.ConcatenateAliases.Models;
using FeBuddy.Core.Application.Airac.Departures.Models;
using FeBuddy.Core.Application.Airac.Fixes.Models;
using FeBuddy.Core.Application.Airac.Navaids.Models;
using FeBuddy.Core.Application.Airac.Procedures.Models;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Application.Airac.WxStations.Models;
using FeBuddy.Core.Application.Models;

namespace FeBuddy.Core.Application.Airac.Models;

/// <summary>
/// The result of one <c>AiracService.RunAsync</c> call: each selected sub-service's own result,
/// plus every message they reported, combined.
/// </summary>
public sealed record AiracServiceResult : ServiceResult
{
	/// <summary>
	/// The cycle folder the run wrote into (<see cref="AiracServiceSettings.CycleOutputDirectory"/>).
	/// It exists only if the run wrote something.
	/// </summary>
	public required string OutputDirectory { get; init; }

	/// <summary>
	/// The duplicate alias report (<c>Duplicate_Alias_Commands.txt</c>) this run wrote, or
	/// <see langword="null"/> when the run wrote no alias file, so there was nothing to check.
	/// </summary>
	public DuplicateAliasReportResult? DuplicateAliasReport { get; init; }

	/// <summary>
	/// The choices the user made for duplicated alias commands when the run stopped for them
	/// (<see cref="AiracServiceSettings.ReviewDuplicateAliases"/>), to save for later runs; empty when
	/// it didn't stop.
	/// </summary>
	public IReadOnlyList<DuplicateAliasRule> DuplicateAliasChoicesMade { get; init; } = [];

	/// <summary>
	/// The saved choices for no duplicated command of this run's - its duplicate has gone - which the
	/// run didn't use.
	/// </summary>
	public IReadOnlyList<DuplicateAliasRule> UnusedDuplicateAliasChoices { get; init; } = [];

	/// <summary>
	/// Whether the user stopped the run when it asked about duplicated alias commands. It saved no
	/// alias file and no <c>Combined_Alias.txt</c> then; the other files it had written stay.
	/// </summary>
	public bool StoppedAtDuplicateReview { get; init; }

	/// <summary>
	/// The Airways sub-service result, or <see langword="null"/> when Airways was not part of
	/// this run.
	/// </summary>
	public AirwayServiceResult? Airways { get; init; }

	/// <summary>
	/// The Airports sub-service result, or <see langword="null"/> when Airports was not part of
	/// this run.
	/// </summary>
	public AirportServiceResult? Airports { get; init; }

	/// <summary>
	/// The Departures sub-service result, or <see langword="null"/> when Departures was not part
	/// of this run.
	/// </summary>
	public DepartureServiceResult? Departures { get; init; }

	/// <summary>
	/// The Arrivals sub-service result, or <see langword="null"/> when Arrivals was not part of
	/// this run.
	/// </summary>
	public ArrivalServiceResult? Arrivals { get; init; }

	/// <summary>
	/// The NAVAIDs sub-service result, or <see langword="null"/> when NAVAIDs was not part of
	/// this run.
	/// </summary>
	public NavaidServiceResult? Navaids { get; init; }

	/// <summary>
	/// The ARTCC Boundaries sub-service result, or <see langword="null"/> when ARTCC Boundaries
	/// was not part of this run.
	/// </summary>
	public ArtccBoundaryServiceResult? ArtccBoundaries { get; init; }

	/// <summary>
	/// The Fixes sub-service result, or <see langword="null"/> when Fixes was not part of this
	/// run.
	/// </summary>
	public FixServiceResult? Fixes { get; init; }

	/// <summary>
	/// The Wx Stations sub-service result, or <see langword="null"/> when Wx Stations was not part
	/// of this run.
	/// </summary>
	public WxStationServiceResult? WxStations { get; init; }

	/// <summary>
	/// The Procedures sub-service result, or <see langword="null"/> when Procedures was not part of
	/// this run.
	/// </summary>
	public ProcedureServiceResult? Procedures { get; init; }

	/// <summary>
	/// The Telephony sub-service result, or <see langword="null"/> when Telephony was not part of
	/// this run.
	/// </summary>
	public TelephonyServiceResult? Telephony { get; init; }

	/// <summary>
	/// What writing <c>Aliases\Combined_Alias.txt</c> produced, or <see langword="null"/> when it was
	/// not tried: the run wrote no alias file and Add Custom Aliases wasn't in it, or the run stopped
	/// at the duplicate alias commands.
	/// </summary>
	public CombinedAliasResult? CombinedAlias { get; init; }

	/// <summary>
	/// Whether each sub-service's own alias file was saved in the cycle's <c>Aliases</c> folder
	/// (<see cref="AiracServiceSettings.ExportIndividualAliasFiles"/>). When it wasn't, no sub-service's result
	/// names its alias file - it was only ever in FE-Buddy's scratch area - and its commands are only in
	/// <c>Combined_Alias.txt</c>.
	/// </summary>
	public bool IndividualAliasFilesExported { get; init; }

	/// <summary>
	/// The file a sub-service's alias commands were saved in: its own alias file, or <c>Combined_Alias.txt</c>
	/// when its own wasn't exported (<see cref="IndividualAliasFilesExported"/>).
	/// </summary>
	/// <param name="aliasFilePath">The sub-service's alias file, as its result names it.</param>
	/// <param name="commandCount">How many alias commands (or lines) it made.</param>
	/// <returns>
	/// The file's name, e.g. <c>Airports.txt</c>; <see langword="null"/> when none were saved: it made none, or
	/// the run stopped at the duplicate review.
	/// </returns>
	public string? AliasCommandsSavedIn(string? aliasFilePath, int commandCount) =>
		aliasFilePath is not null ? Path.GetFileName(aliasFilePath)
			: !IndividualAliasFilesExported && commandCount > 0 && CombinedAlias?.FilePath is { } combined ? Path.GetFileName(combined)
			: null;

	/// <summary>
	/// What a sub-service's summary says about its alias commands, naming the file they are in: e.g.
	/// <c>Airports.txt: 1,234 alias command(s)</c>, or <c>1,234 alias command(s), in Combined_Alias.txt</c> when
	/// its own file wasn't exported.
	/// </summary>
	/// <param name="aliasFilePath">The sub-service's alias file, as its result names it.</param>
	/// <param name="commandCount">How many alias commands (or lines) it made.</param>
	/// <param name="what">What follows the count, e.g. <c>alias command(s)</c>.</param>
	/// <returns>The words; <see langword="null"/> when no command was saved (<see cref="AliasCommandsSavedIn"/>).</returns>
	public string? DescribeAliasCommands(string? aliasFilePath, int commandCount, string what) =>
		AliasCommandsSavedIn(aliasFilePath, commandCount) is not { } file ? null
			: aliasFilePath is not null ? $"{file}: {commandCount:N0} {what}"
			: $"{commandCount:N0} {what}, in {file}";

	/// <summary>
	/// Every file the run wrote, in sub-service order: each sub-service's GeoJSON and documents and its alias
	/// file, then the duplicate alias report and the combined alias file. A run that didn't export the
	/// sub-services' alias files (<see cref="IndividualAliasFilesExported"/>), or stopped at the duplicate review,
	/// names none of them.
	/// </summary>
	public IReadOnlyList<string> FilesWritten
	{
		get
		{
			List<string> files = [];

			if (Airways is { } airways)
			{
				files.AddRange(airways.GeojsonFilesWritten);

				if (airways.AliasFilePath is { } airwayAlias)
				{
					files.Add(airwayAlias);
				}
			}

			if (Airports is { } airports)
			{
				files.AddRange(airports.GeojsonFilesWritten);

				if (airports.AliasFilePath is { } airportAlias)
				{
					files.Add(airportAlias);
				}
			}

			if (Departures is { } departures)
			{
				files.AddRange(departures.GeojsonFilesWritten);

				if (departures.AliasFilePath is { } departureAlias)
				{
					files.Add(departureAlias);
				}
			}

			if (Arrivals is { } arrivals)
			{
				files.AddRange(arrivals.GeojsonFilesWritten);

				if (arrivals.AliasFilePath is { } arrivalAlias)
				{
					files.Add(arrivalAlias);
				}
			}

			if (Navaids is { } navaids)
			{
				files.AddRange(navaids.GeojsonFilesWritten);

				if (navaids.AliasFilePath is { } navaidAlias)
				{
					files.Add(navaidAlias);
				}
			}

			if (ArtccBoundaries is { } artccBoundaries)
			{
				files.AddRange(artccBoundaries.GeojsonFilesWritten);
			}

			if (Fixes is { } fixes)
			{
				files.AddRange(fixes.GeojsonFilesWritten);
			}

			if (WxStations is { } wxStations)
			{
				files.AddRange(wxStations.GeojsonFilesWritten);
			}

			if (Procedures is { } procedures)
			{
				files.AddRange(procedures.FilesWritten);

				if (procedures.AliasFilePath is { } chartRecallAlias)
				{
					files.Add(chartRecallAlias);
				}
			}

			if (Telephony?.AliasFilePath is { } telephonyAlias)
			{
				files.Add(telephonyAlias);
			}

			if (DuplicateAliasReport is { } duplicateAliasReport)
			{
				files.Add(duplicateAliasReport.FilePath);
			}

			if (CombinedAlias?.FilePath is { } combinedAlias)
			{
				files.Add(combinedAlias);
			}

			return [.. files];
		}
	}
}

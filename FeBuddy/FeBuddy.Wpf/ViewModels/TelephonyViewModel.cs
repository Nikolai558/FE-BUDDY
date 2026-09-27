using System.IO;

using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Infrastructure.Nasr.Models;
using FeBuddy.Core.Infrastructure.Telephony;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// The <b>Telephony</b> sub-service tab inside the AIRAC Service screen: the <c>Telephony.txt</c>
/// alias file and whether it goes to vNAS.
/// </summary>
/// <remarks>
/// <para>
/// The alias file is Telephony's only output, and it covers every operator in the FAA telephony
/// pages, so there is nothing else to choose. The tab still derives from
/// <see cref="GeojsonSubServiceViewModel"/> for the alias file and the Upload to vNAS card, with
/// <see cref="EmitKeys"/> <c>(null, null, null)</c> and one output that cannot be turned off.
/// </para>
/// <para>
/// Like Wx Stations, its data does not come from the selected cycle at all: every run downloads the
/// latest FAA telephony pages into one kept copy, whichever cycle is run - see
/// <see cref="RefreshTelephonyData"/>. Save, Undo and navigation come from the tab host's action
/// bar; the run is launched by <b>Run AIRAC Service</b> on the Preview Settings tab, and its
/// results are shown on the Review tab, described by this tab through
/// <see cref="ISubServiceRunTarget"/>.
/// </para>
/// </remarks>
public sealed class TelephonyViewModel : GeojsonSubServiceViewModel, ISubServiceRunTarget
{
	private const string Node = "Services.AiracService.Telephony";

	private string _telephonyDataStatus = string.Empty;

	/// <summary>Builds the tab and restores its saved settings.</summary>
	public TelephonyViewModel()
	{
		LoadFromConfig();
		RefreshTelephonyData();
	}

	/// <inheritdoc />
	public override string NodePath => Node;

	/// <inheritdoc />
	public override string Title => "Telephony";

	// ================= outputs =================

	/// <inheritdoc />
	/// <remarks>The alias file is the only output, so it can never be turned off here.</remarks>
	protected override int EnabledOutputCount => 1;

	/// <inheritdoc />
	protected override string NoDefaultRoiHint => "Telephony is not limited to a region.";

	/// <inheritdoc />
	/// <remarks>Telephony writes no GeoJSON at all.</remarks>
	protected override (string? Lines, string? Symbols, string? Text) EmitKeys => (null, null, null);

	// ================= telephony data =================

	/// <summary>What the Telephony Data card and the Preview Settings tab say about FE-Buddy's kept copies of the FAA pages.</summary>
	public string TelephonyDataStatus => _telephonyDataStatus;

	/// <summary>
	/// Re-reads how old FE-Buddy's kept copy of the FAA telephony register is
	/// (<see cref="TelephonyFiles.RegisterFilePath"/>). Called when the tab is built and by the AIRAC
	/// Service screen after every run, which is when the copies are replaced.
	/// </summary>
	/// <remarks>
	/// Never blocks the run: a missing copy is what the run downloads, and a failed download is
	/// reported by the run itself.
	/// </remarks>
	public void RefreshTelephonyData()
	{
		_telephonyDataStatus = File.Exists(TelephonyFiles.RegisterFilePath)
			? $"FE-Buddy's copy is from {File.GetLastWriteTime(TelephonyFiles.RegisterFilePath):d MMM yyyy}. Every run downloads the latest pages first, and uses this copy only if it can't."
			: "FE-Buddy has no copy yet. Every run downloads the latest pages first, so the first run needs an internet connection.";

		OnPropertyChanged(nameof(TelephonyDataStatus));
	}

	// ================= parent hooks =================

	/// <inheritdoc />
	/// <remarks>Does nothing: Telephony has no lists built from the selected cycle's NASR data.</remarks>
	public void LoadCycleDependentLists(NasrCsvDataCollection data)
	{
	}

	/// <inheritdoc />
	public SubServiceRunResult? DescribeRunResult(AiracServiceResult result)
	{
		if (result.Telephony is not { } telephony)
		{
			return null;
		}

		string summary = telephony.AliasFilePath is not null
			? $"{TelephonyOutputFiles.Alias}: {telephony.AliasCommandCount:N0} command(s) for {telephony.IcaoAssignmentCount:N0} ICAO operator(s) " +
				$"and {telephony.SpecialCallSignCount:N0} U.S. special call sign(s)"
			: "Nothing written";

		return new SubServiceRunResult(Title, summary, telephony.Messages);
	}

	/// <inheritdoc />
	/// <remarks>The block goes on <see cref="AiracServiceSettings.Telephony"/>.</remarks>
	public IReadOnlyDictionary<string, string> BuildSettingsBlock()
	{
		Dictionary<string, string> s = new(StringComparer.OrdinalIgnoreCase);
		AddSharedSettings(s);
		return s;
	}

	/// <inheritdoc />
	public override IReadOnlyList<ServicePreviewSection> BuildPreviewSummary()
	{
		ServicePreviewRow[] rows =
		[
			new ServicePreviewRow("Alias file", $"{TelephonyOutputFiles.Alias}, every operator in the FAA telephony pages"),
			new ServicePreviewRow("Telephony data", TelephonyDataStatus),
			new ServicePreviewRow("Upload to vNAS", DescribeVnasFiles()),
		];

		return [new ServicePreviewSection("Telephony", rows)];
	}

	// ================= save contract =================

	/// <inheritdoc />
	protected override void LoadFromConfig()
	{
		LoadSharedSettings();

		// The alias file is the only output and the tab has no switch for it, so a hand-edited "N"
		// would leave the tab producing nothing; fall back to the default rather than honour it.
		if (!GenerateAliasFile)
		{
			GenerateAliasFile = true;
		}

		ClearDirty();
	}

	/// <inheritdoc />
	protected override void WriteToConfig() => SaveSharedSettings();

	/// <inheritdoc />
	protected override void Validate(ServiceValidation validation) => ValidateSharedSettings(validation);

	/// <inheritdoc />
	/// <remarks>Only the alias file; with no GeoJSON there is nothing to carry CRC-ERAM defaults.</remarks>
	protected override IEnumerable<OutputFileOption> OutputFiles()
	{
		yield return OutputFileOption.AliasFile(TelephonyOutputFiles.Alias);
	}
}

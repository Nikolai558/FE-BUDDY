using System.Text;

using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="VatsimRadarVirtualAirlines"/> and <see cref="VirtualAirline.IsSameAs"/>: merging
/// GNG's list with VATSIM-Radar's; which of the list's virtual airlines can be written (and which are
/// left out), each once and in order; what counts as the same virtual airline (all three values,
/// ignoring case and spaces at either end) and the same call sign (3LD and telephony); and reading the
/// kept copies - both parts, one alone, the older single copy, none.
/// </summary>
public sealed class VatsimRadarVirtualAirlinesTests : IDisposable
{
	private readonly string _folder = Path.Combine(Path.GetTempPath(), "FeBuddyTests_VatsimRadarList_" + Guid.NewGuid().ToString("N"));

	public void Dispose()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private string Write(string fileName, string content)
	{
		Directory.CreateDirectory(_folder);
		string path = Path.Combine(_folder, fileName);
		File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		return path;
	}

	// ---- Select ----

	/// <summary>Real entries from the list: C, PHENX and TROY have no three-letter designator, so no card.</summary>
	[Fact]
	public void a_virtual_airline_that_could_not_be_one_of_yours_is_left_out_and_named()
	{
		VatsimRadarSelection selection = VatsimRadarVirtualAirlines.Select(
		[
			new VatsimRadarAirline("PHENX", "Phoenix AirV", "PHOENIX"),
			new VatsimRadarAirline("DAL", "Fly Delta Virtual", "Delta"),
			new VatsimRadarAirline("C", "United States Coast Guard Virtual", "COAST GUARD"),
			new VatsimRadarAirline("TROY", "US Department of Homeland Security", "TROY"),
			new VatsimRadarAirline("NUL", "No Telephony", "!!"),
		]);

		Assert.Equal([new VirtualAirline("DAL", "Delta", "Fly Delta Virtual")], selection.VirtualAirlines);
		Assert.Equal(["C", "NUL", "PHENX", "TROY"], selection.LeftOut);
	}

	[Fact]
	public void each_virtual_airline_is_written_once_sorted_by_3ld_telephony_and_organization()
	{
		VatsimRadarSelection selection = VatsimRadarVirtualAirlines.Select(
		[
			new VatsimRadarAirline("WEX", "World Express B", "WORLD EXPRESS"),
			new VatsimRadarAirline("ASK", "AIRSKY", "AIRSKY"),
			new VatsimRadarAirline("WEX", "World Express A", "WORLD EXPRESS"),
			new VatsimRadarAirline("ask", "airsky", "airsky"),
		]);

		Assert.Equal(
			["ASK / AIRSKY / AIRSKY", "WEX / WORLD EXPRESS / World Express A", "WEX / WORLD EXPRESS / World Express B"],
			selection.VirtualAirlines.Select(va => $"{va.Designator} / {va.Telephony} / {va.Organization}"));
		Assert.Empty(selection.LeftOut);
	}

	// ---- the same virtual airline ----

	[Theory]
	[InlineData("DAL", "Delta", "Fly Delta Virtual", true)]
	[InlineData(" dal ", " DELTA ", " fly delta virtual ", true)]
	[InlineData("DAL", "Delta", "Delta Virtual", false)]
	[InlineData("DVA", "Delta", "Fly Delta Virtual", false)]
	[InlineData("DAL", "Delta Air", "Fly Delta Virtual", false)]
	public void the_same_virtual_airline_has_the_same_3ld_telephony_and_organization_ignoring_case(
		string designator, string telephony, string organization, bool expected)
	{
		VirtualAirline listed = new("DAL", "Delta", "Fly Delta Virtual");

		Assert.Equal(expected, listed.IsSameAs(designator, telephony, organization));
		Assert.Equal(expected ? listed : null, VatsimRadarVirtualAirlines.FindSame([listed], designator, telephony, organization));
	}

	/// <summary>The same call sign is the same 3LD and telephony, ignoring case and spaces - whatever the virtual organization.</summary>
	[Theory]
	[InlineData("DAL", "Delta", true)]
	[InlineData(" dal ", " DELTA ", true)]
	[InlineData("DVA", "Delta", false)]
	[InlineData("DAL", "Delta Air", false)]
	public void the_same_call_sign_has_the_same_3ld_and_telephony_whatever_the_organization(string designator, string telephony, bool expected)
	{
		VirtualAirline listed = new("DAL", "Delta", "Fly Delta Virtual");

		Assert.Equal(expected, listed.HasSameCallSign(designator, telephony));
		Assert.Equal(expected ? listed : null, VatsimRadarVirtualAirlines.FindSameCallSign([listed], designator, telephony));
	}

	[Fact]
	public void select_and_find_same_reject_a_null_list()
	{
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.Select(null!));
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.FindSame(null!, "DAL", "Delta", "Fly Delta Virtual"));
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.FindSameCallSign(null!, "DAL", "Delta"));
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.Merge(null!, []));
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.Merge([], null!));
	}

	// ---- Merge ----

	/// <summary>
	/// As VATSIM-Radar merges them: GNG's rows, duplicates kept; a VATSIM-Radar entry takes over the name
	/// and telephony of the first GNG row with its 3LD; one GNG doesn't have is added at the end.
	/// </summary>
	[Fact]
	public void merge_replaces_the_first_gng_row_with_the_3ld_and_appends_the_rest()
	{
		List<VatsimRadarAirline> merged = VatsimRadarVirtualAirlines.Merge(
			[
				new VatsimRadarAirline("SKA", "SKY AIR", "SKYAIR"),
				new VatsimRadarAirline("SKA", "SKYALLIANCE", "SKYALLIANCE"),
				new VatsimRadarAirline("VIR", "VIRTUAL VIRGIN", "VIRGINV"),
			],
			[
				new VatsimRadarAirline(" ska ", "Sky Virtual", "SKYV"),
				new VatsimRadarAirline("OCN", "vOCN", "Ocean"),
			]);

		Assert.Equal(
			[
				new VatsimRadarAirline("SKA", "Sky Virtual", "SKYV"),
				new VatsimRadarAirline("SKA", "SKYALLIANCE", "SKYALLIANCE"),
				new VatsimRadarAirline("VIR", "VIRTUAL VIRGIN", "VIRGINV"),
				new VatsimRadarAirline("OCN", "vOCN", "Ocean"),
			],
			merged);
	}

	/// <summary>A VATSIM-Radar entry only ever takes over a GNG row, never one of its own added before it.</summary>
	[Fact]
	public void merge_matches_only_gng_rows()
	{
		List<VatsimRadarAirline> merged = VatsimRadarVirtualAirlines.Merge(
			[],
			[new VatsimRadarAirline("OCN", "vOCN", "Ocean"), new VatsimRadarAirline("OCN", "Ocean Virtual", "Ocean")]);

		Assert.Equal(2, merged.Count);
	}

	// ---- ReadKeptCopy ----

	private static string GngJson(params string[] rows)
	{
		IEnumerable<string> all = rows.Concat(Enumerable.Range(0, 100 - rows.Length)
			.Select(i => $$"""{ "icao": "Q{{i:00}}", "airline": "FILLER {{i}}", "callsign": "FILLER{{i}}" }"""));

		return $$"""{ "records": 100, "rows": [{{string.Join(",", all)}}] }""";
	}

	private static readonly DateTime GngUtc = new(2026, 10, 2, 8, 0, 0, DateTimeKind.Utc);

	private static readonly DateTime VatsimRadarUtc = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

	private string WriteDated(string fileName, string content, DateTime downloadedUtc)
	{
		string path = Write(fileName, content);
		File.SetLastWriteTimeUtc(path, downloadedUtc);
		return path;
	}

	/// <summary>Both parts: merged, then what a run would write from them - the 3LDs that aren't three letters left out - and when each was downloaded.</summary>
	[Fact]
	public void the_kept_copies_give_what_a_run_would_write_and_when_each_was_downloaded()
	{
		string gng = WriteDated("gng.json", GngJson("""{ "icao": "WAT", "airline": "WALKER AIR", "callsign": "WALKER" }"""), GngUtc);
		string vatsimRadar = WriteDated("vr.json", """[{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }]""", VatsimRadarUtc);

		VatsimRadarCopy copy = Assert.IsType<VatsimRadarCopy>(VatsimRadarVirtualAirlines.ReadKeptCopy(gng, vatsimRadar, olderCopyPath: null));

		Assert.Equal(
			[new VirtualAirline("DAL", "Delta", "Fly Delta Virtual"), new VirtualAirline("WAT", "WALKER", "WALKER AIR")],
			copy.VirtualAirlines);
		Assert.Equal(VatsimRadarUtc, copy.DownloadedUtc);
		Assert.Equal(GngUtc, copy.GngDownloadedUtc);
		Assert.Equal(VatsimRadarUtc, copy.VatsimRadarDownloadedUtc);
		Assert.False(copy.IsOlderCopy);
	}

	/// <summary>One part alone - the other missing or unreadable - is still the list; the older copy isn't read.</summary>
	[Fact]
	public void one_part_alone_is_the_list_and_the_older_copy_is_not_read()
	{
		string vatsimRadar = WriteDated("vr.json", """[{ "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true }]""", VatsimRadarUtc);
		string older = Write("older.json", """{ "virtual": [{ "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true }] }""");

		VatsimRadarCopy copy = Assert.IsType<VatsimRadarCopy>(VatsimRadarVirtualAirlines.ReadKeptCopy(Write("gng.json", "<html></html>"), vatsimRadar, older));

		Assert.Equal([new VirtualAirline("DAL", "Delta", "Fly Delta Virtual")], copy.VirtualAirlines);
		Assert.Null(copy.GngDownloadedUtc);
		Assert.False(copy.IsOlderCopy);
	}

	/// <summary>The older single copy - beta.4's merged list or beta.3's GitHub array - is read only while neither part can be.</summary>
	[Theory]
	[InlineData("""{ "airlines": [], "virtual": [{ "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true }] }""")]
	[InlineData("""[{ "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true }]""")]
	public void the_older_copy_is_read_only_while_neither_part_can_be(string olderJson)
	{
		string older = WriteDated("older.json", olderJson, VatsimRadarUtc);

		VatsimRadarCopy copy = Assert.IsType<VatsimRadarCopy>(VatsimRadarVirtualAirlines.ReadKeptCopy(Path.Combine(_folder, "none.json"), null, older));

		Assert.Equal([new VirtualAirline("OCN", "Ocean", "vOCN")], copy.VirtualAirlines);
		Assert.True(copy.IsOlderCopy);
		Assert.Equal(VatsimRadarUtc, copy.DownloadedUtc);
	}

	[Fact]
	public void no_kept_copy_or_one_that_cannot_be_read_is_none()
	{
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Path.Combine(_folder, "missing.json"), null, null));
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Write("broken.json", "<html></html>"), Write("object.json", "{}"), Write("older.json", "{}")));
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Write("short.json", """{ "records": 1, "rows": [{ "icao": "DAL", "airline": "X", "callsign": "Y" }] }"""), null, null));
	}
}

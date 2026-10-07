using System.Text;

using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Infrastructure.Telephony.Models;

namespace FeBuddy.UnitTests.Application.Airac.Telephony;

/// <summary>
/// Covers <see cref="VatsimRadarVirtualAirlines"/> and <see cref="VirtualAirline.IsSameAs"/>: which
/// of the list's virtual airlines can be written (and which are left out), each once and in order;
/// what counts as the same virtual airline (all three values, ignoring case and spaces at either
/// end); and reading a kept copy - none, a good one, one that cannot be read.
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

	[Fact]
	public void select_and_find_same_reject_a_null_list()
	{
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.Select(null!));
		Assert.Throws<ArgumentNullException>(() => VatsimRadarVirtualAirlines.FindSame(null!, "DAL", "Delta", "Fly Delta Virtual"));
	}

	// ---- ReadKeptCopy ----

	[Fact]
	public void a_kept_copy_gives_what_a_run_would_write_and_when_it_was_downloaded()
	{
		string path = Write("airlines.json", """
			{
			  "virtual": [
			    { "icao": "DAL", "name": "Fly Delta Virtual", "callsign": "Delta", "virtual": true },
			    { "icao": "C", "name": "Coast Guard Virtual", "callsign": "COAST GUARD", "virtual": true }
			  ]
			}
			""");
		DateTime downloadedUtc = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
		File.SetLastWriteTimeUtc(path, downloadedUtc);

		VatsimRadarCopy copy = Assert.IsType<VatsimRadarCopy>(VatsimRadarVirtualAirlines.ReadKeptCopy(path));

		Assert.Equal([new VirtualAirline("DAL", "Delta", "Fly Delta Virtual")], copy.VirtualAirlines);
		Assert.Equal(downloadedUtc, copy.DownloadedUtc);
	}

	[Fact]
	public void no_kept_copy_or_one_that_cannot_be_read_is_none()
	{
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Path.Combine(_folder, "missing.json")));
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Write("broken.json", "<html></html>")));
		Assert.Null(VatsimRadarVirtualAirlines.ReadKeptCopy(Write("object.json", "{}")));
	}
}

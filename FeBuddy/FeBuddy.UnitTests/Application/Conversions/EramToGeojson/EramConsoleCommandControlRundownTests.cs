using FeBuddy.Core.Application.Conversions.EramToGeojson;
using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.UnitTests.Application.Conversions.EramToGeojson;

/// <summary>
/// Covers <see cref="EramConsoleCommandControlRundown"/>: <c>ConsoleCommandControl.txt</c> laid out
/// exactly as the original ERAM_2_GEOJSON tool laid it out, and where it is written.
/// </summary>
public sealed class EramConsoleCommandControlRundownTests
{
	private static readonly EramConsoleCommandControl Menus = new(
		[
			new EramMapMenu("ZOB",
			[
				new EramMapMenuButton("1", "AAV", null, ["1", "2"]),
				new EramMapMenuButton("2", "SECT", null, ["3"]),
			]),
			new EramMapMenu("SPARE", []),
		],
		[
			new EramMapMenu("ZOB",
			[
				new EramMapMenuButton("1", "HI", "ALT", ["1", "2"]),
				new EramMapMenuButton("2", "LO", null, []),
			]),
		]);

	/// <summary>CENTER is listed once though two records name it; a map with no filter menu is in no filter menu's list.</summary>
	private static readonly EramGeoMap[] Maps =
	[
		new("CENTER", null, null, []) { BcgMenuName = "ZOB", FilterMenuName = "ZOB" },
		new("OCP", null, null, []) { BcgMenuName = "ZOB" },
		new("CENTER", null, null, []) { BcgMenuName = "ZOB" },
		new(string.Empty, null, null, []) { BcgMenuName = "ZOB" },
	];

	[Fact]
	public void the_rundown_is_laid_out_as_the_original_tool_laid_it_out()
	{
		string[] lines =
		[
			":::::::::::::::::::::::::::::::::::",
			"::   Brightness Control Groups   ::",
			":::::::::::::::::::::::::::::::::::",
			"",
			"BCG Menu: ZOB",
			"",
			"\tUsed with:\tCENTER, OCP",
			"",
			"\tLabel:\t\tAAV",
			"\tPosition:\t1",
			"\tGroup:\t\t1, 2",
			"",
			"\tLabel:\t\tSECT",
			"\tPosition:\t2",
			"\tGroup:\t\t3",
			"",
			"",
			"BCG Menu: SPARE",
			"",
			"\tUsed with:\tNone",
			"",
			"",
			":::::::::::::::::::::::::::::::::::",
			"::        Filter Groups          ::",
			":::::::::::::::::::::::::::::::::::",
			"",
			"FilterMenu: ZOB",
			"",
			"\tUsed with:\tCENTER",
			"",
			"\tLabel:\t\tHI",
			"\t\t\t\tALT",
			"\tPosition:\t1",
			"\tGroup:\t\t1, 2",
			"",
			"\tLabel:\t\tLO",
			"\tPosition:\t2",
			"\tGroup:\t\t",
			"",
			"",
		];

		Assert.Equal(string.Concat(lines.Select(line => line + "\r\n")), EramConsoleCommandControlRundown.Format(Menus, Maps));
	}

	[Fact]
	public void a_file_with_no_menus_has_just_the_two_headings()
	{
		string text = EramConsoleCommandControlRundown.Format(new EramConsoleCommandControl([], []), Maps);

		Assert.Equal(8, text.Split("\r\n").Length - 1);
		Assert.Contains("::        Filter Groups          ::", text);
	}

	[Fact]
	public void write_creates_the_folder_and_writes_utf8_without_a_byte_order_mark()
	{
		string folder = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}", "ERAM_TO_GEOJSON");

		try
		{
			string path = EramConsoleCommandControlRundown.Write(Menus, Maps, folder);

			Assert.Equal(Path.Combine(folder, "ConsoleCommandControl.txt"), path);
			Assert.Equal((byte)':', File.ReadAllBytes(path)[0]);
			Assert.Equal(EramConsoleCommandControlRundown.Format(Menus, Maps), File.ReadAllText(path));
		}
		finally
		{
			Directory.Delete(Path.GetDirectoryName(folder)!, recursive: true);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentNullException>(() => EramConsoleCommandControlRundown.Write(null!, Maps, "x"));
		Assert.Throws<ArgumentNullException>(() => EramConsoleCommandControlRundown.Write(Menus, null!, "x"));
		Assert.Throws<ArgumentException>(() => EramConsoleCommandControlRundown.Write(Menus, Maps, " "));
	}
}

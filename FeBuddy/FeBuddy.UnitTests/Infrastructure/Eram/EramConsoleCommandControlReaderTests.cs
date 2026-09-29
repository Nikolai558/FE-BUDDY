using System.Text;

using FeBuddy.Core.Infrastructure.Eram;
using FeBuddy.Core.Infrastructure.Eram.Models;

namespace FeBuddy.UnitTests.Infrastructure.Eram;

/// <summary>
/// Covers <see cref="EramConsoleCommandControlReader"/>: the brightness and filter menus with their
/// buttons, what is left out, and what is not a ConsoleCommandControl file. Every file here is made up.
/// </summary>
public sealed class EramConsoleCommandControlReaderTests
{
	private static EramConsoleCommandControl Parse(string xml) =>
		EramConsoleCommandControlReader.Parse(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "TEST.xml");

	[Fact]
	public void reads_the_brightness_and_filter_menus_with_their_buttons()
	{
		EramConsoleCommandControl menus = Parse("""
			<ConsoleCommandControl_Records xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
			  <!-- Local SITE ID : ZXX -->
			  <MapBrightnessMenu>
			    <BCGMenuName> ZXX </BCGMenuName>
			    <MapBCGButton>
			      <MenuPosition>1</MenuPosition><Label>AAV</Label>
			      <MapBCGGroups><MapBCGGroup>1</MapBCGGroup><MapBCGGroup> 2 </MapBCGGroup><MapBCGGroup> </MapBCGGroup></MapBCGGroups>
			    </MapBCGButton>
			    <MapBCGButton><MenuPosition>2</MenuPosition></MapBCGButton>
			  </MapBrightnessMenu>
			  <MapBrightnessMenu />
			  <MapFilterMenu>
			    <FilterMenuName>ZXXF</FilterMenuName>
			    <MapFilterButton>
			      <MenuPosition>3</MenuPosition><DefaultSetting>ON</DefaultSetting><LabelLine1>HI</LabelLine1><LabelLine2>ALT</LabelLine2>
			      <MapFilterGroups><MapFilterGroup>4</MapFilterGroup></MapFilterGroups>
			    </MapFilterButton>
			    <MapFilterButton><LabelLine1>LO</LabelLine1><LabelLine2 /></MapFilterButton>
			  </MapFilterMenu>
			  <SDKeypadKeys><KeypadKey>A</KeypadKey></SDKeypadKeys>
			</ConsoleCommandControl_Records>
			""");

		Assert.Equal(["ZXX", string.Empty], menus.BrightnessMenus.Select(m => m.Name));
		Assert.Empty(menus.BrightnessMenus[1].Buttons);

		EramMapMenuButton[] bcg = [.. menus.BrightnessMenus[0].Buttons];
		Assert.Equal(("1", "AAV", (string?)null), (bcg[0].Position, bcg[0].LabelLine1, bcg[0].LabelLine2));
		Assert.Equal(["1", "2"], bcg[0].Groups);
		Assert.Equal(("2", (string?)null), (bcg[1].Position, bcg[1].LabelLine1));
		Assert.Empty(bcg[1].Groups);

		EramMapMenu filters = Assert.Single(menus.FilterMenus);
		Assert.Equal("ZXXF", filters.Name);
		Assert.Equal(("3", "HI", "ALT"), (filters.Buttons[0].Position, filters.Buttons[0].LabelLine1, filters.Buttons[0].LabelLine2));
		Assert.Equal(["4"], filters.Buttons[0].Groups);
		Assert.Equal(((string?)null, "LO", (string?)null), (filters.Buttons[1].Position, filters.Buttons[1].LabelLine1, filters.Buttons[1].LabelLine2));
	}

	[Fact]
	public void a_file_with_no_menus_has_none()
	{
		EramConsoleCommandControl menus = Parse("<ConsoleCommandControl_Records />");

		Assert.Empty(menus.BrightnessMenus);
		Assert.Empty(menus.FilterMenus);
	}

	[Fact]
	public void a_file_that_is_not_a_console_command_control_file_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<Geomaps_Records />"));

		Assert.Equal("TEST.xml is not an ERAM ConsoleCommandControl file: it starts with <Geomaps_Records>, not <ConsoleCommandControl_Records>.", error.Message);
	}

	[Fact]
	public void a_file_that_is_not_well_formed_is_rejected()
	{
		InvalidDataException error = Assert.Throws<InvalidDataException>(() => Parse("<ConsoleCommandControl_Records><MapFilterMenu>"));

		Assert.StartsWith("TEST.xml is not well-formed XML", error.Message, StringComparison.Ordinal);
	}

	[Fact]
	public void read_loads_a_file_from_disk_and_tells_it_from_the_rest_of_an_export()
	{
		string folder = Path.Combine(Path.GetTempPath(), $"FeBuddyTests_{Guid.NewGuid():N}");
		Directory.CreateDirectory(folder);
		string ccc = Path.Combine(folder, "ConsoleCommandControl.xml");
		string geomaps = Path.Combine(folder, "Geomaps.xml");
		string broken = Path.Combine(folder, "Broken.xml");
		File.WriteAllText(ccc, "<ConsoleCommandControl_Records><MapFilterMenu><FilterMenuName>ZXX</FilterMenuName></MapFilterMenu></ConsoleCommandControl_Records>");
		File.WriteAllText(geomaps, "<Geomaps_Records />");
		File.WriteAllText(broken, "not xml");

		try
		{
			Assert.Equal("ZXX", Assert.Single(EramConsoleCommandControlReader.Read(ccc).FilterMenus).Name);

			Assert.True(EramConsoleCommandControlReader.IsConsoleCommandControlFile(ccc));
			Assert.False(EramConsoleCommandControlReader.IsConsoleCommandControlFile(geomaps));
			Assert.False(EramConsoleCommandControlReader.IsConsoleCommandControlFile(broken));
			Assert.False(EramConsoleCommandControlReader.IsConsoleCommandControlFile(Path.Combine(folder, "Missing.xml")));
		}
		finally
		{
			Directory.Delete(folder, recursive: true);
		}
	}

	[Fact]
	public void bad_arguments_are_rejected()
	{
		Assert.Throws<ArgumentException>(() => EramConsoleCommandControlReader.Read(" "));
		Assert.Throws<ArgumentNullException>(() => EramConsoleCommandControlReader.Parse(null!, "x"));
	}
}

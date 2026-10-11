using System.Globalization;
using System.Windows.Data;

using FeBuddy.Wpf.Converters;

namespace FeBuddy.UnitTests.Wpf.Converters;

/// <summary>
/// Covers <see cref="FileNamesAsCodeConverter"/>: the Review tab shows each file name in a run
/// message as code, and leaves names in code, paths and web addresses alone.
/// </summary>
public sealed class FileNamesAsCodeConverterTests
{
	[Theory]
	[InlineData(
		"Left custom alias file 2 (Extra.txt) out of Combined_Alias.txt: GitHub could not find it.",
		"Left custom alias file 2 (`Extra.txt`) out of `Combined_Alias.txt`: GitHub could not find it.")]
	[InlineData("Wrote ARTCC-Boundary_High_Lines.geojson.", "Wrote `ARTCC-Boundary_High_Lines.geojson`.")]
	[InlineData("Procedure_Changes.md and Procedures.json", "`Procedure_Changes.md` and `Procedures.json`")]
	[InlineData("Read Geomaps.xml and ZOB.SCT2", "Read `Geomaps.xml` and `ZOB.SCT2`")]
	public void file_names_become_code(string message, string expected)
	{
		Assert.Equal(expected, FileNamesAsCodeConverter.Mark(message));
	}

	[Theory]
	[InlineData("Already `Combined_Alias.txt` here.")]
	[InlineData(@"Written to C:\Output\AIRAC_2610\Aliases\Airways.txt")]
	[InlineData("Downloaded https://github.com/o/r/blob/main/ZOB.txt")]
	[InlineData("<Airport ID>_<Departure ID>_Lines.geojson")]
	[InlineData("12 GeoJSON file(s), 3 airport(s), e.g. v3.0.0")]
	[InlineData("")]
	public void names_in_code_paths_and_addresses_are_left_alone(string message)
	{
		Assert.Equal(message, FileNamesAsCodeConverter.Mark(message));
	}

	[Fact]
	public void the_converter_marks_strings_and_turns_anything_else_into_nothing()
	{
		FileNamesAsCodeConverter converter = new();

		Assert.Equal("`a.txt`", converter.Convert("a.txt", typeof(string), null, CultureInfo.InvariantCulture));
		Assert.Equal(string.Empty, converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture));
		Assert.Equal(Binding.DoNothing, converter.ConvertBack("x", typeof(string), null, CultureInfo.InvariantCulture));
		Assert.Throws<ArgumentNullException>(() => FileNamesAsCodeConverter.Mark(null!));
	}
}

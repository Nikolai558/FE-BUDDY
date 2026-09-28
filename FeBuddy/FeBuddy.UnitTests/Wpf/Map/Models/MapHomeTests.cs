using System.Globalization;

using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.UnitTests.Wpf.Map.Models;

/// <summary>Covers <see cref="MapHome"/>: the saved form of the home view, whatever the PC's culture.</summary>
public sealed class MapHomeTests
{
	[Fact]
	public void the_home_view_round_trips_through_its_saved_form()
	{
		MapHome home = new(34.05, -118.25, 6.5);

		Assert.Equal("34.05,-118.25,6.5", home.ToConfig());
		Assert.Equal(home, MapHome.Parse(home.ToConfig()));
	}

	/// <summary>A PC that writes 6,5 for six and a half still saves and reads the invariant form.</summary>
	[Fact]
	public void the_saved_form_ignores_the_pcs_culture()
	{
		CultureInfo original = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
			MapHome home = new(50.1, 8.7, 7.25);

			Assert.Equal("50.1,8.7,7.25", home.ToConfig());
			Assert.Equal(home, MapHome.Parse("50.1,8.7,7.25"));
		}
		finally
		{
			CultureInfo.CurrentCulture = original;
		}
	}

	/// <summary>Anything missing, malformed or off the map reads as no home view (the default).</summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("34.05,-118.25")]
	[InlineData("34.05,-118.25,6.5,1")]
	[InlineData("north,-118.25,6.5")]
	[InlineData("86,-118.25,6.5")]
	[InlineData("34.05,181,6.5")]
	[InlineData("34.05,-118.25,19")]
	[InlineData("34.05,-118.25,-1")]
	public void a_bad_saved_value_is_no_home(string? saved) => Assert.Null(MapHome.Parse(saved));
}

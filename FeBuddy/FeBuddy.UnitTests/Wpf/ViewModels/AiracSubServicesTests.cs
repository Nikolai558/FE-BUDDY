using FeBuddy.Wpf.ViewModels;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers <see cref="AiracSubServices"/>: the order the General tab's table and the tab rail show
/// the sub-services in.
/// </summary>
public sealed class AiracSubServicesTests
{
	[Fact]
	public void the_sub_services_are_in_the_rail_order()
	{
		Assert.Equal(
			[
				"ARTCC Boundaries", "Airports", "Airways", "Arrivals", "Departures", "NAVAIDs",
				"Fixes", "Procedure Charts", "Telephony", "Wx Stations", "Add Custom Aliases",
			],
			AiracSubServices.All.Select(s => s.DisplayName));
	}

	[Fact]
	public void every_key_is_unique()
	{
		Assert.Equal(AiracSubServices.All.Count, AiracSubServices.All.Select(s => s.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
	}
}

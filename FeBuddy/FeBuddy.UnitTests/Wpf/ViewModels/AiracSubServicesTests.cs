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

	/// <summary>The table and the rail sort by Order, and other code walks the list as it is, so the two must agree.</summary>
	[Fact]
	public void each_order_value_is_higher_than_the_last()
	{
		int[] orders = [.. AiracSubServices.All.Select(s => s.Order)];

		Assert.Equal(orders.Order(), orders);
		Assert.Equal(orders.Length, orders.Distinct().Count());
	}

	[Fact]
	public void every_key_is_unique()
	{
		Assert.Equal(AiracSubServices.All.Count, AiracSubServices.All.Select(s => s.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
	}
}

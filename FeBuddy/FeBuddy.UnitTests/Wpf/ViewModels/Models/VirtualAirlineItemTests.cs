using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels.Models;

/// <summary>
/// Covers <see cref="VirtualAirlineItem"/>, one line of the Telephony tab's Virtual Airlines list:
/// its label, the commands it shows under, and the change notifications that keep both current while
/// the editor changes it in place.
/// </summary>
public sealed class VirtualAirlineItemTests
{
	/// <summary>The names of the properties the item reports changed from now on.</summary>
	private static List<string?> Watch(VirtualAirlineItem item)
	{
		List<string?> raised = [];
		item.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
		return raised;
	}

	[Fact]
	public void the_label_is_the_3ld_and_the_telephony()
	{
		VirtualAirlineItem item = new("DVA", "DELTA", "Delta Virtual");

		Assert.Equal("DVA · DELTA", item.Label);
		Assert.Equal("DVA", item.Designator);
		Assert.Equal("DELTA", item.Telephony);
		Assert.Equal("Delta Virtual", item.Organization);
	}

	[Theory]
	[InlineData("DVA", "DELTA", ".idDVA and .idDELTA")]
	[InlineData("DVA", "RYAN AIR", ".idDVA and .idRYANAIR")]
	[InlineData("NAS", "NAS", ".idNAS")]
	[InlineData("nas", "NAS", ".idNAS")]
	[InlineData("DVA", "!!", ".idDVA")]
	[InlineData("!!", "DELTA", ".idDELTA")]
	[InlineData("!!", "--", "")]
	public void the_commands_are_the_3ld_and_the_telephony_without_repeats_or_what_cannot_be_typed(
		string designator, string telephony, string expected)
	{
		VirtualAirlineItem item = new(designator, telephony, "Some Virtual");

		Assert.Equal(expected, item.Commands);
	}

	[Fact]
	public void changing_the_designator_raises_the_label_and_the_commands()
	{
		VirtualAirlineItem item = new("DVA", "DELTA", "Delta Virtual");
		List<string?> raised = Watch(item);

		item.Designator = "DEV";

		Assert.Equal([nameof(VirtualAirlineItem.Designator), nameof(VirtualAirlineItem.Label), nameof(VirtualAirlineItem.Commands)], raised);
		Assert.Equal("DEV · DELTA", item.Label);
		Assert.Equal(".idDEV and .idDELTA", item.Commands);
	}

	[Fact]
	public void changing_the_telephony_raises_the_label_and_the_commands()
	{
		VirtualAirlineItem item = new("DVA", "DELTA", "Delta Virtual");
		List<string?> raised = Watch(item);

		item.Telephony = "DEVIL AIR";

		Assert.Equal([nameof(VirtualAirlineItem.Telephony), nameof(VirtualAirlineItem.Label), nameof(VirtualAirlineItem.Commands)], raised);
		Assert.Equal("DVA · DEVIL AIR", item.Label);
		Assert.Equal(".idDVA and .idDEVILAIR", item.Commands);
	}

	/// <summary>The organization is on the card but not in the label or the commands, so only it is reported.</summary>
	[Fact]
	public void changing_the_organization_raises_only_the_organization()
	{
		VirtualAirlineItem item = new("DVA", "DELTA", "Delta Virtual");
		List<string?> raised = Watch(item);

		item.Organization = "Rustic Virtual";

		Assert.Equal([nameof(VirtualAirlineItem.Organization)], raised);
		Assert.Equal("DVA · DELTA", item.Label);
	}

	[Fact]
	public void setting_a_value_it_already_has_raises_nothing()
	{
		VirtualAirlineItem item = new("DVA", "DELTA", "Delta Virtual");
		List<string?> raised = Watch(item);

		item.Designator = "DVA";
		item.Telephony = "DELTA";
		item.Organization = "Delta Virtual";

		Assert.Empty(raised);
	}
}

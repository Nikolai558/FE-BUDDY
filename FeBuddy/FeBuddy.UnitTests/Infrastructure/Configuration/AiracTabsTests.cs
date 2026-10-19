using FeBuddy.Core.Infrastructure.Configuration;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>Covers <see cref="AiracTabs"/>: each AIRAC tab's key and settings node, spelled once.</summary>
public sealed class AiracTabsTests
{
	[Fact]
	public void a_tab_has_its_node_and_a_node_its_tab()
	{
		Assert.Equal("Services.AiracService.Airways", AiracTabs.NodeFor(AiracTabs.Airways));
		Assert.Equal(AiracTabs.Procedures, AiracTabs.TabOf("Services.AiracService.Procedures"));
		Assert.Null(AiracTabs.TabOf("Services.AiracService.Procedures.Facilities"));
		Assert.Null(AiracTabs.TabOf("Services.MapService"));
		Assert.Null(AiracTabs.TabOf("Services.AiracService."));
		Assert.Equal(AiracTabs.Procedures, UserConfigAreas.ProceduresTab);
		Assert.All(UserConfigAreas.Tabs.Keys, tab => Assert.Equal(tab, AiracTabs.TabOf(AiracTabs.NodeFor(tab))));
		Assert.Throws<ArgumentNullException>(() => AiracTabs.NodeFor(null!));
		Assert.Throws<ArgumentNullException>(() => AiracTabs.TabOf(null!));
	}
}

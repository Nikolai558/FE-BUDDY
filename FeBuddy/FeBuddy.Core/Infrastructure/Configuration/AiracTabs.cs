namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// The AIRAC Service tabs' keys: each tab's name for its settings node under
/// <see cref="UserConfigAreas.AiracNode"/>, e.g. <c>Services.AiracService.Airways</c>, and the key the GUI
/// knows a sub-service by. Spelled once, here, for Core's settings rules and every tab.
/// </summary>
public static class AiracTabs
{
	/// <summary>The General tab.</summary>
	public const string General = "General";

	/// <summary>The File Names tab.</summary>
	public const string FileNames = "FileNames";

	/// <summary>Airports.</summary>
	public const string Airports = "Airports";

	/// <summary>Airways.</summary>
	public const string Airways = "Airways";

	/// <summary>Departures.</summary>
	public const string Departures = "Departures";

	/// <summary>Arrivals.</summary>
	public const string Arrivals = "Arrivals";

	/// <summary>NAVAIDs.</summary>
	public const string Navaids = "Navaids";

	/// <summary>ARTCC Boundaries.</summary>
	public const string ArtccBoundaries = "ArtccBoundaries";

	/// <summary>Fixes.</summary>
	public const string Fixes = "Fixes";

	/// <summary>Wx Stations.</summary>
	public const string WxStations = "WxStations";

	/// <summary>Procedure Charts.</summary>
	public const string Procedures = "Procedures";

	/// <summary>Telephony.</summary>
	public const string Telephony = "Telephony";

	/// <summary>Add Custom Aliases.</summary>
	public const string ConcatenateAliases = "ConcatenateAliases";

	/// <summary>A tab's settings node, e.g. <c>Services.AiracService.Airways</c>.</summary>
	/// <param name="tab">The tab's key, e.g. <see cref="Airways"/>.</param>
	/// <returns>The node's dotted path.</returns>
	public static string NodeFor(string tab)
	{
		ArgumentNullException.ThrowIfNull(tab);
		return $"{UserConfigAreas.AiracNode}.{tab}";
	}

	/// <summary>The tab a settings node is for: <c>Airways</c> for <c>Services.AiracService.Airways</c>.</summary>
	/// <param name="node">The node's dotted path.</param>
	/// <returns>The tab's key, or <see langword="null"/> when the node isn't directly under <see cref="UserConfigAreas.AiracNode"/>.</returns>
	public static string? TabOf(string node)
	{
		ArgumentNullException.ThrowIfNull(node);

		string prefix = UserConfigAreas.AiracNode + ".";
		string tab = node.StartsWith(prefix, StringComparison.Ordinal) ? node[prefix.Length..] : string.Empty;

		return tab.Length > 0 && !tab.Contains('.') ? tab : null;
	}
}

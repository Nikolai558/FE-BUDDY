namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The keys for problems that belong to a whole card rather than one box
/// (<see cref="ServiceValidation.AddArea"/>). The card binds its key as
/// <c>bhv:FieldState.Error="{Binding FieldErrors[GeojsonFiles]}"</c>, which outlines it in red
/// while the problem lasts. A card is also outlined while any box inside it has a field error, so
/// a card whose problems all belong to its boxes needs no key.
/// </summary>
public static class ServiceAreas
{
	/// <summary>The What Files Do You Want? card (GeojsonFilesCard).</summary>
	public const string GeojsonFiles = "GeojsonFiles";

	/// <summary>The FE-Buddy Properties card (FebPropertiesCard).</summary>
	public const string FebProperties = "FebProperties";

	/// <summary>The CRC ERAM Defaults card (CrcDefaultsCard).</summary>
	public const string CrcDefaults = "CrcDefaults";

	/// <summary>The Region of Interest card (RoiOverrideCard).</summary>
	public const string Roi = "Roi";

	/// <summary>The High and Low Files card (Airways tab).</summary>
	public const string HighAndLowFiles = "HighAndLowFiles";

	/// <summary>The Fix Uses card (Fixes tab).</summary>
	public const string FixUses = "FixUses";

	/// <summary>The Charts card (Fixes tab).</summary>
	public const string Charts = "Charts";

	/// <summary>The Combinations card (Fixes tab).</summary>
	public const string Combinations = "Combinations";

	/// <summary>The NAVAID Types card (NAVAIDs tab).</summary>
	public const string NavaidTypes = "NavaidTypes";

	/// <summary>
	/// The cards that pick what the Procedures documents cover: Facilities, Airports, Procedures at
	/// Any Airport and Airport + Procedure (Procedures tab).
	/// </summary>
	public const string DocumentSelection = "DocumentSelection";

	/// <summary>The Chart Types card (Procedures tab).</summary>
	public const string ChartTypes = "ChartTypes";

	/// <summary>The Virtual Airlines card (Telephony tab).</summary>
	public const string VirtualAirlines = "VirtualAirlines";

	/// <summary>The Custom Alias Files card (Concatenate Aliases tab).</summary>
	public const string CustomAliasFiles = "CustomAliasFiles";

	/// <summary>The Output Files card (File Names tab).</summary>
	public const string OutputFiles = "OutputFiles";
}

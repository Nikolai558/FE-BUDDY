namespace FeBuddy.Core.Application.Airac.Fixes.Models;

/// <summary>How the Fixes sub-service groups its GeoJSON output into files.</summary>
public enum FixOutputBy
{
	/// <summary>One Symbols file and one Text file for every included fix: <c>Fix_Symbols</c> / <c>Fix_Text</c>.</summary>
	All = 0,

	/// <summary>One Symbols file and one Text file per fix use present, e.g. <c>Fix_WYPNT_Symbols</c> / <c>Fix_WYPNT_Text</c>.</summary>
	FixUse = 1,

	/// <summary>One Symbols file and one Text file per chart present, e.g. <c>Fix_ENROUTE-LOW_Symbols</c> / <c>Fix_ENROUTE-LOW_Text</c>.</summary>
	Chart = 2,

	/// <summary>
	/// One Symbols file and one Text file per user-listed chart + fix use combination
	/// (<c>Combinations</c>), e.g. <c>Fix_ENROUTE-LOW-WYPNT_Symbols</c> / <c>Fix_ENROUTE-LOW-WYPNT_Text</c>.
	/// </summary>
	ChartAndFixUse = 3,
}

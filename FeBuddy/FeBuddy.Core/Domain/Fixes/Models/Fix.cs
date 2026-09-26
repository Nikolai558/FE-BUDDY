namespace FeBuddy.Core.Domain.Fixes.Models;

/// <summary>
/// One fix or reporting point, built from a single <c>FIX_BASE</c> row.
/// </summary>
/// <remarks>
/// Duplicate <see cref="FixId"/> values are allowed in real NASR data, even though the current
/// cycle has none - every row NASR publishes becomes its own <see cref="Fix"/>, never merged or
/// de-duplicated.
/// </remarks>
/// <param name="FixId">Fix identifier, <c>FIX_BASE.FIX_ID</c> (e.g. <c>ACME</c>). Never blank.</param>
/// <param name="Latitude">Fix latitude in decimal degrees, <c>FIX_BASE.LAT_DECIMAL</c>.</param>
/// <param name="Longitude">Fix longitude in decimal degrees, <c>FIX_BASE.LONG_DECIMAL</c>.</param>
/// <param name="FixUseCode">The raw NASR type-of-use code, <c>FIX_BASE.FIX_USE_CODE</c> (e.g. <c>WP</c>).</param>
/// <param name="FixUse">
/// The fix's type of use, mapped from <paramref name="FixUseCode"/> by <see cref="FixUses.Name"/>
/// (e.g. <c>WYPNT</c> for <c>WP</c>).
/// </param>
/// <param name="Charts">
/// The NASR chart names the fix is depicted on, <c>FIX_BASE.CHARTS</c>, parsed by
/// <see cref="FixCharts.Parse"/>. Empty when NASR publishes none.
/// </param>
public sealed record Fix(
	string FixId,
	double Latitude,
	double Longitude,
	string FixUseCode,
	string FixUse,
	IReadOnlyList<string> Charts);

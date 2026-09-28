namespace FeBuddy.Core.Domain.Navaids.Models;

/// <summary>
/// One NAVAID, built from a single <c>NAV_BASE</c> row.
/// </summary>
/// <remarks>
/// Duplicate <see cref="NavId"/> values (and duplicate <see cref="Name"/> values) are normal in
/// real NASR data - about 71 identifiers repeat, e.g. <c>ABQ</c> is both a VORTAC and a VOT - so
/// every row NASR publishes becomes its own <see cref="Navaid"/>, never merged or de-duplicated.
/// </remarks>
/// <param name="NavId">NAVAID identifier, <c>NAV_BASE.NAV_ID</c> (e.g. <c>ABQ</c>). Never blank.</param>
/// <param name="NavType">
/// NAVAID type, <c>NAV_BASE.NAV_TYPE</c>, trimmed and upper-cased (e.g. <c>VORTAC</c>). See
/// <see cref="NavaidTypes"/> for the known vocabulary.
/// </param>
/// <param name="Name">NAVAID name, <c>NAV_BASE.NAME</c>, trimmed.</param>
/// <param name="Freq">
/// The frequency the NAVAID transmits on, <c>NAV_BASE.FREQ</c>, or <see langword="null"/> when
/// NASR publishes none (e.g. a TACAN, which transmits on a channel instead).
/// </param>
/// <param name="Latitude">NAVAID latitude in decimal degrees, <c>NAV_BASE.LAT_DECIMAL</c>.</param>
/// <param name="Longitude">NAVAID longitude in decimal degrees, <c>NAV_BASE.LONG_DECIMAL</c>.</param>
/// <param name="LowAltArtccId">
/// Identifier of the ARTCC whose low altitude boundary the NAVAID falls within,
/// <c>NAV_BASE.LOW_ALT_ARTCC_ID</c>, trimmed - an empty string when NASR publishes none.
/// </param>
/// <param name="HighAltArtccId">
/// Identifier of the ARTCC whose high altitude boundary the NAVAID falls within,
/// <c>NAV_BASE.HIGH_ALT_ARTCC_ID</c>, trimmed - an empty string when NASR publishes none.
/// </param>
public sealed record Navaid(
	string NavId,
	string NavType,
	string Name,
	double? Freq,
	double Latitude,
	double Longitude,
	string LowAltArtccId,
	string HighAltArtccId);

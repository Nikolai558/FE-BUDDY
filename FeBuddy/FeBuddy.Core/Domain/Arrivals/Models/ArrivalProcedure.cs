namespace FeBuddy.Core.Domain.Arrivals.Models;

/// <summary>
/// One standard terminal arrival as NASR publishes it (one <c>STAR_BASE</c> row plus its
/// <c>STAR_APT</c> and <c>STAR_RTE</c> rows), before it is split per airport or located on the
/// map.
/// </summary>
/// <remarks>
/// <para>
/// Unlike a departure, <c>STAR_APT</c> and <c>STAR_RTE</c> carry no arrival name at all - only
/// <c>STAR_COMPUTER_CODE</c> and <c>ARTCC</c> - so the three STAR tables are joined on those two
/// columns instead, both trimmed and compared ignoring case. Once joined, a procedure is
/// identified by airport + <see cref="CodeId"/> - see <see cref="ArrivalAirportProcedure"/>.
/// </para>
/// </remarks>
public sealed record ArrivalProcedure
{
	/// <summary>The published name (<c>ARRIVAL_NAME</c>), e.g. <c>BLAID</c>, <c>FERNANDO</c>, <c>WESTMINSTER</c>.</summary>
	public required string ArrivalName { get; init; }

	/// <summary>
	/// The <c>ARTCC</c> column exactly as published, trimmed. Usually one centre, but sometimes
	/// several separated by spaces, e.g. <c>"ZDC ZNY"</c>. Used for messages and as half of the
	/// join key with <c>STAR_APT</c> and <c>STAR_RTE</c>.
	/// </summary>
	public required string ArtccText { get; init; }

	/// <summary>
	/// <see cref="ArtccText"/> split on whitespace, trimmed, upper-cased and de-duplicated, in the
	/// order published. Almost always one entry; two for a STAR shared by neighbouring centres
	/// (e.g. <c>ARLFT</c>, <c>ENO.ARLFT2</c>, serving <c>33N</c>, <c>DOV</c> and <c>ILG</c>, is
	/// <c>"ZDC ZNY"</c>).
	/// </summary>
	public required IReadOnlyList<string> Artccs { get; init; }

	/// <summary>
	/// For a procedure shared by more than one ARTCC (<see cref="Artccs"/>.Count &gt; 1), each
	/// served airport's own ARTCC (<c>APT_BASE.RESP_ARTCC_ID</c>, trimmed and upper-cased) when it
	/// is one of <see cref="Artccs"/>. Empty when the procedure belongs to a single ARTCC.
	/// </summary>
	public required IReadOnlyDictionary<string, string> ArtccByAirport { get; init; }

	/// <summary>The FAA computer code as published (<c>STAR_COMPUTER_CODE</c>), e.g. <c>AALAN.BLAID2</c> or <c>NOT ASSIGNED</c>.</summary>
	public required string ComputerCode { get; init; }

	/// <summary>
	/// The identifier FE-Buddy uses for this procedure in file names and alias commands, e.g.
	/// <c>BLAID</c>, <c>FERN</c>, <c>EMI</c>. Letters and digits only.
	/// </summary>
	/// <remarks>Derived by <c>ArrivalNaming.CodeIdFor</c>; see there for the rule and its fallback.</remarks>
	public required string CodeId { get; init; }

	/// <summary>The amendment, spelled out (<c>AMENDMENT_NO</c>), e.g. <c>TWO</c>.</summary>
	public required string AmendmentNo { get; init; }

	/// <summary>The first effective date of the current amendment as published (<c>STAR_AMEND_EFF_DATE</c>, <c>YYYY/MM/DD</c>).</summary>
	public required string AmendmentEffectiveDateText { get; init; }

	/// <summary><see cref="AmendmentEffectiveDateText"/> parsed, or <see langword="null"/> when it is not a valid date.</summary>
	public DateOnly? AmendmentEffectiveDate { get; init; }

	/// <summary>The NASR subscription effective date of the data this row came from (<c>EFF_DATE</c>), parsed.</summary>
	public DateOnly? CycleEffectiveDate { get; init; }

	/// <summary>
	/// Every airport the procedure serves: the union of <c>STAR_BASE.SERVED_ARPT</c> and every
	/// <c>STAR_APT.ARPT_ID</c>, in that order, without duplicates.
	/// </summary>
	/// <remarks>
	/// The two lists can disagree for a handful of procedures, so taking both errs on the side of
	/// producing the procedure for an airport rather than silently dropping it.
	/// </remarks>
	public required IReadOnlyList<string> ServedAirports { get; init; }

	/// <summary>The procedure's bodies, in the order <c>STAR_RTE</c> lists them.</summary>
	public required IReadOnlyList<ArrivalRawRoute> Bodies { get; init; }

	/// <summary>The procedure's transitions, in the order <c>STAR_RTE</c> lists them.</summary>
	public required IReadOnlyList<ArrivalRawRoute> Transitions { get; init; }

	/// <summary>
	/// The body names (and their <c>BODY_SEQ</c>) that <c>STAR_APT</c> assigns to each airport,
	/// keyed by <c>ARPT_ID</c>. A body is matched to a <see cref="Bodies"/> entry by name
	/// (ignoring case) and sequence together, because two real STARs reuse a body name with a
	/// different <c>BODY_SEQ</c> to mean two different bodies. An airport missing from this map -
	/// it appears only in <c>SERVED_ARPT</c> - uses every body.
	/// </summary>
	public required IReadOnlyDictionary<string, IReadOnlyList<(string Name, int Sequence)>> BodiesByAirport { get; init; }

	/// <summary>Whether <c>STAR_RTE</c> has any rows for this procedure. A procedure without them produces no output.</summary>
	public bool HasRoutes => Bodies.Count > 0 || Transitions.Count > 0;

	/// <summary>
	/// The ARTCC an airport's copy of the arrival belongs to. For a STAR shared by two ARTCCs,
	/// this is the one responsible for the airport (<c>APT_BASE.RESP_ARTCC_ID</c>) rather than the
	/// whole procedure - e.g. <c>ARLFT</c> is <c>ZDC</c> at <c>DOV</c> but <c>ZNY</c> at <c>ILG</c>.
	/// </summary>
	/// <param name="airportId">The FAA identifier of the airport.</param>
	/// <returns>
	/// <see cref="ArtccByAirport"/>'s value for the airport when there is one; otherwise the
	/// first (and, for almost every procedure, only) entry of <see cref="Artccs"/>; or
	/// <see cref="string.Empty"/> when neither is available.
	/// </returns>
	public string ArtccFor(string airportId) =>
		ArtccByAirport.TryGetValue(airportId, out string? artcc) ? artcc
		: Artccs.Count > 0 ? Artccs[0]
		: string.Empty;
}

/// <summary>One body or transition as listed in <c>STAR_RTE</c>, not yet located.</summary>
/// <param name="Name">The route name (<c>ROUTE_NAME</c>), e.g. <c>AALAN-BLAID</c> or <c>FIM TRANSITION</c>.</param>
/// <param name="Sequence">The body sequence (<c>BODY_SEQ</c>), which together with <paramref name="Name"/> identifies the route.</param>
/// <param name="Kind">Body or transition.</param>
/// <param name="TransitionCode">The transition computer code (<c>TRANSITION_COMPUTER_CODE</c>), or <see langword="null"/> for a body.</param>
/// <param name="Points">The route's points in <c>POINT_SEQ</c> order.</param>
public sealed record ArrivalRawRoute(
	string Name,
	int Sequence,
	ArrivalRouteKind Kind,
	string? TransitionCode,
	IReadOnlyList<ArrivalRawPoint> Points);

/// <summary>One point on a route as listed in <c>STAR_RTE</c>, not yet located.</summary>
/// <param name="Id">The point identifier (<c>POINT</c>), e.g. <c>BLAID</c> or <c>LAS</c>.</param>
/// <param name="PointType">The point type with NASR's trailing padding removed (<c>POINT_TYPE</c>), e.g. <c>WP</c>, <c>VORTAC</c>.</param>
public sealed record ArrivalRawPoint(string Id, string PointType);

/// <summary>One point on an arrival, located from FIX_BASE or NAV_BASE.</summary>
/// <param name="Id">The point identifier.</param>
/// <param name="PointType">The point type, e.g. <c>WP</c>, <c>VORTAC</c>.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
public sealed record ArrivalPoint(string Id, string PointType, double Latitude, double Longitude);

/// <summary>A located body or transition.</summary>
/// <param name="Name">The route name.</param>
/// <param name="Kind">Body or transition.</param>
/// <param name="Points">The route's located points, in sequence.</param>
public sealed record ArrivalRoute(string Name, ArrivalRouteKind Kind, IReadOnlyList<ArrivalPoint> Points);

/// <summary>
/// One procedure as it applies to one airport, fully located: the unit every output is written
/// for (one set of GeoJSON files, one alias command).
/// </summary>
/// <remarks>
/// Two airports sharing a procedure name get two of these, and they are allowed to differ: each
/// airport gets only the bodies <c>STAR_APT</c> assigns it, plus every transition.
/// </remarks>
public sealed record ArrivalAirportProcedure
{
	/// <summary>The procedure this is a copy of.</summary>
	public required ArrivalProcedure Procedure { get; init; }

	/// <summary>The FAA identifier of the airport, e.g. <c>LAS</c>.</summary>
	public required string AirportId { get; init; }

	/// <summary>
	/// The ARTCC this copy of the arrival belongs to (<see cref="ArrivalProcedure.ArtccFor"/> for
	/// <see cref="AirportId"/>).
	/// </summary>
	public required string Artcc { get; init; }

	/// <summary>This airport's transitions, then its bodies, each located - the order they are flown.</summary>
	public required IReadOnlyList<ArrivalRoute> Routes { get; init; }

	/// <summary>Every point across <see cref="Routes"/>, once each, in the order first met.</summary>
	public required IReadOnlyList<ArrivalPoint> Points { get; init; }

	/// <summary>
	/// Whether there is a line to draw - at least one route with two different points. A
	/// procedure that is a single fix gets Symbols, Text and an alias command, but no Lines file.
	/// </summary>
	public bool HasDrawableRoute => Routes.Any(route =>
		route.Points.Select(point => point.Id).Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any());
}

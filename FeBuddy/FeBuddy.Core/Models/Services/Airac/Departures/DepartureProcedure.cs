namespace FeBuddy.Core.Models.Services.Airac.Departures;

/// <summary>
/// One departure procedure as NASR publishes it (one <c>DP_BASE</c> row plus its <c>DP_APT</c>
/// and <c>DP_RTE</c> rows), before it is split per airport or located on the map.
/// </summary>
/// <remarks>
/// <para>
/// The three DP tables are joined on <see cref="DpName"/> + <see cref="Artcc"/>. The name alone
/// is not unique (PORTLAND is both a ZBW and a ZSE procedure), <c>DP_RTE</c> carries no airport
/// to join on instead, and <c>DP_COMPUTER_CODE</c> is the literal text <c>NOT ASSIGNED</c> on
/// dozens of procedures. Once joined, a procedure is identified by airport +
/// <see cref="CodeId"/> - see <see cref="DepartureAirportProcedure"/>.
/// </para>
/// </remarks>
public sealed record DepartureProcedure
{
	/// <summary>The published name (<c>DP_NAME</c>), e.g. <c>DOTSS</c>, <c>SALT LAKE</c>, <c>HUDSN (COPTER)</c>.</summary>
	public required string DpName { get; init; }

	/// <summary>The ARTCC the procedure belongs to (<c>ARTCC</c>).</summary>
	public required string Artcc { get; init; }

	/// <summary>The FAA computer code as published (<c>DP_COMPUTER_CODE</c>), e.g. <c>DOTSS2.DOTSS</c> or <c>NOT ASSIGNED</c>.</summary>
	public required string ComputerCode { get; init; }

	/// <summary>
	/// The identifier FE-Buddy uses for this procedure in file names and alias commands, e.g.
	/// <c>DOTSS</c>, <c>SLC</c>, <c>OHARE</c>. Letters and digits only.
	/// </summary>
	/// <remarks>Derived by <c>DepartureNaming.CodeIdFor</c>; see there for the rule and its fallback.</remarks>
	public required string CodeId { get; init; }

	/// <summary>The amendment, spelled out (<c>AMENDMENT_NO</c>), e.g. <c>TWO</c>.</summary>
	public required string AmendmentNo { get; init; }

	/// <summary>The first effective date of the current amendment as published (<c>DP_AMEND_EFF_DATE</c>, <c>YYYY/MM/DD</c>).</summary>
	public required string AmendmentEffectiveDateText { get; init; }

	/// <summary><see cref="AmendmentEffectiveDateText"/> parsed, or <see langword="null"/> when it is not a valid date.</summary>
	public DateOnly? AmendmentEffectiveDate { get; init; }

	/// <summary>The NASR subscription effective date of the data this row came from (<c>EFF_DATE</c>), parsed.</summary>
	public DateOnly? CycleEffectiveDate { get; init; }

	/// <summary><see langword="true"/> for an obstacle departure (<c>GRAPHICAL_DP_TYPE</c> of <c>OBSTACLE</c>), <see langword="false"/> for a SID.</summary>
	public required bool IsObstacleDeparture { get; init; }

	/// <summary>
	/// Every airport the procedure serves: the union of <c>DP_BASE.SERVED_ARPT</c> and every
	/// <c>DP_APT.ARPT_ID</c>, in that order, without duplicates.
	/// </summary>
	/// <remarks>
	/// The two lists disagree for a handful of procedures (CONLE, FOXHL, TRMML), so taking both
	/// errs on the side of producing the procedure for an airport rather than silently dropping
	/// it.
	/// </remarks>
	public required IReadOnlyList<string> ServedAirports { get; init; }

	/// <summary>The procedure's bodies, in the order <c>DP_RTE</c> lists them.</summary>
	public required IReadOnlyList<DepartureRawRoute> Bodies { get; init; }

	/// <summary>The procedure's transitions, in the order <c>DP_RTE</c> lists them.</summary>
	public required IReadOnlyList<DepartureRawRoute> Transitions { get; init; }

	/// <summary>
	/// The body names <c>DP_APT</c> assigns to each airport (<c>BODY_NAME</c> by
	/// <c>ARPT_ID</c>). An airport missing from this map - it appears only in
	/// <c>SERVED_ARPT</c> - uses every body.
	/// </summary>
	public required IReadOnlyDictionary<string, IReadOnlyList<string>> BodyNamesByAirport { get; init; }

	/// <summary>Whether <c>DP_RTE</c> has any rows for this procedure. A procedure without them produces no output.</summary>
	public bool HasRoutes => Bodies.Count > 0 || Transitions.Count > 0;
}

/// <summary>One body or transition as listed in <c>DP_RTE</c>, not yet located.</summary>
/// <param name="Name">The route name (<c>ROUTE_NAME</c>), e.g. <c>DLREY-DOTSS</c> or <c>CLEEE TRANSITION</c>.</param>
/// <param name="Kind">Body or transition.</param>
/// <param name="TransitionCode">The transition computer code (<c>TRANSITION_COMPUTER_CODE</c>), or <see langword="null"/> for a body.</param>
/// <param name="Points">The route's points in <c>POINT_SEQ</c> order.</param>
public sealed record DepartureRawRoute(
	string Name,
	DepartureRouteKind Kind,
	string? TransitionCode,
	IReadOnlyList<DepartureRawPoint> Points);

/// <summary>One point on a route as listed in <c>DP_RTE</c>, not yet located.</summary>
/// <param name="Id">The point identifier (<c>POINT</c>), e.g. <c>DOTSS</c> or <c>LAX</c>.</param>
/// <param name="PointType">The point type with NASR's trailing padding removed (<c>POINT_TYPE</c>), e.g. <c>WP</c>, <c>VORTAC</c>.</param>
public sealed record DepartureRawPoint(string Id, string PointType);

/// <summary>One point on a departure, located from FIX_BASE or NAV_BASE.</summary>
/// <param name="Id">The point identifier.</param>
/// <param name="PointType">The point type, e.g. <c>WP</c>, <c>VORTAC</c>.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
public sealed record DeparturePoint(string Id, string PointType, double Latitude, double Longitude);

/// <summary>A located body or transition.</summary>
/// <param name="Name">The route name.</param>
/// <param name="Kind">Body or transition.</param>
/// <param name="Points">The route's located points, in sequence.</param>
public sealed record DepartureRoute(string Name, DepartureRouteKind Kind, IReadOnlyList<DeparturePoint> Points);

/// <summary>
/// One procedure as it applies to one airport, fully located: the unit every output is written
/// for (one set of GeoJSON files, one alias command).
/// </summary>
/// <remarks>
/// Two airports sharing a procedure name get two of these, and they are allowed to differ: each
/// airport gets only the bodies <c>DP_APT</c> assigns it, plus every transition.
/// </remarks>
public sealed record DepartureAirportProcedure
{
	/// <summary>The procedure this is a copy of.</summary>
	public required DepartureProcedure Procedure { get; init; }

	/// <summary>The FAA identifier of the airport, e.g. <c>LAX</c>.</summary>
	public required string AirportId { get; init; }

	/// <summary>This airport's bodies, then every transition, each located.</summary>
	public required IReadOnlyList<DepartureRoute> Routes { get; init; }

	/// <summary>Every point across <see cref="Routes"/>, once each, in the order first met.</summary>
	public required IReadOnlyList<DeparturePoint> Points { get; init; }

	/// <summary>
	/// Whether there is a line to draw - at least one route with two different points. A
	/// procedure that is a single fix gets Symbols, Text and an alias command, but no Lines file.
	/// </summary>
	public bool HasDrawableRoute => Routes.Any(route =>
		route.Points.Select(point => point.Id).Distinct(StringComparer.OrdinalIgnoreCase).Skip(1).Any());
}

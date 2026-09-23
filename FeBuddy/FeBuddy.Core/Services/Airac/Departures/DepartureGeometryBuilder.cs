using FeBuddy.Core.Handlers.General;
using FeBuddy.Core.Models.Services.Airac.Departures;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Services.Airac.Departures;

/// <summary>
/// Builds the single MultiLineString a departure's Lines file holds, using Efficient Linestring
/// Handling so segments shared by several bodies and transitions are drawn once.
/// </summary>
/// <remarks>
/// <para>
/// Before the shared segments are merged, each body is joined to every transition that starts
/// at its last point, so the longest continuous run - runway fix to en-route fix - becomes the
/// base LineString. On DOTSS2 at LAX that is one body run straight on into a transition, with
/// the other bodies and transitions added only where they leave it.
/// </para>
/// <para>
/// A transition that starts part-way along a body, or at no body point at all (a radar-vector
/// SID has no body), is kept as its own path; the merge still joins it wherever it shares a
/// point with what is already drawn.
/// </para>
/// </remarks>
internal static class DepartureGeometryBuilder
{
	/// <summary>
	/// Builds the geometry for one airport + procedure.
	/// </summary>
	/// <param name="airportProcedure">The located airport + procedure.</param>
	/// <param name="factory">The geometry factory.</param>
	/// <returns>The MultiLineString, or <see langword="null"/> when there is no segment to draw.</returns>
	internal static MultiLineString? Build(DepartureAirportProcedure airportProcedure, GeometryFactory factory)
	{
		ArgumentNullException.ThrowIfNull(airportProcedure);
		ArgumentNullException.ThrowIfNull(factory);

		IReadOnlyList<LineString> lines = EfficientLinestringHandler.Build(Paths(airportProcedure.Routes), factory);

		return lines.Count == 0 ? null : factory.CreateMultiLineString(lines.ToArray());
	}

	/// <summary>
	/// The paths handed to the merge: each body extended by every transition that begins at its
	/// last point, then any transition that did not extend a body.
	/// </summary>
	/// <param name="routes">The airport's bodies and transitions.</param>
	/// <returns>The paths, as keyed coordinates.</returns>
	internal static List<IReadOnlyList<(string Key, Coordinate Coordinate)>> Paths(IReadOnlyList<DepartureRoute> routes)
	{
		List<DepartureRoute> bodies = routes.Where(r => r.Kind == DepartureRouteKind.Body && r.Points.Count > 0).ToList();
		List<DepartureRoute> transitions = routes.Where(r => r.Kind == DepartureRouteKind.Transition && r.Points.Count > 0).ToList();

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = new();
		HashSet<DepartureRoute> usedTransitions = new(ReferenceEqualityComparer.Instance);

		foreach (DepartureRoute body in bodies)
		{
			string lastId = body.Points[^1].Id;
			List<DepartureRoute> continuing = transitions
				.Where(t => string.Equals(t.Points[0].Id, lastId, StringComparison.OrdinalIgnoreCase))
				.ToList();

			if (continuing.Count == 0)
			{
				paths.Add(ToPath(body.Points));
				continue;
			}

			foreach (DepartureRoute transition in continuing)
			{
				usedTransitions.Add(transition);
				paths.Add(ToPath(body.Points.Concat(transition.Points.Skip(1))));
			}
		}

		foreach (DepartureRoute transition in transitions.Where(t => !usedTransitions.Contains(t)))
		{
			paths.Add(ToPath(transition.Points));
		}

		return paths;
	}

	private static IReadOnlyList<(string Key, Coordinate Coordinate)> ToPath(IEnumerable<DeparturePoint> points) =>
		points.Select(p => (p.Id, new Coordinate(p.Longitude, p.Latitude))).ToList();
}

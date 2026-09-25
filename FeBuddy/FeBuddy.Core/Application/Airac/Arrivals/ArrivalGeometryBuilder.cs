using FeBuddy.Core.Domain.Arrivals.Models;
using FeBuddy.Core.Domain.Geo;

using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Application.Airac.Arrivals;

/// <summary>
/// Builds the single MultiLineString an arrival's Lines file holds, using Efficient Linestring
/// Handling so segments shared by several transitions and bodies are drawn once.
/// </summary>
/// <remarks>
/// <para>
/// An arrival is flown transition to body - the reverse of a departure. Before the shared
/// segments are merged, each body is joined to every transition that ends at its first point, so
/// the longest continuous run - en-route fix to the airport - becomes the base LineString. On
/// BLAID2 at LAS that is the BCE transition (<c>BCE-HOLDM-AALAN</c>) running straight on into the
/// body <c>AALAN-BLAID</c>.
/// </para>
/// <para>
/// A transition that ends part-way along a body, or at no body point at all (a real example:
/// MAKAH1's <c>LAVAS</c> transition ends at <c>HONUU</c>, but the body starts at <c>MAKAH</c>),
/// is kept as its own path; the merge still joins it wherever it shares a point with what is
/// already drawn.
/// </para>
/// </remarks>
internal static class ArrivalGeometryBuilder
{
	/// <summary>
	/// Builds the geometry for one airport + procedure.
	/// </summary>
	/// <param name="airportProcedure">The located airport + procedure.</param>
	/// <returns>The MultiLineString, or <see langword="null"/> when there is no segment to draw.</returns>
	internal static MultiLineString? Build(ArrivalAirportProcedure airportProcedure)
	{
		ArgumentNullException.ThrowIfNull(airportProcedure);

		IReadOnlyList<LineString> lines = LineStringMerger.Merge(Paths(airportProcedure.Routes));

		return lines.Count == 0 ? null : Wgs84.Factory.CreateMultiLineString([.. lines]);
	}

	/// <summary>
	/// The paths handed to the merge: each body preceded by every transition that ends at its
	/// first point, then any transition that did not reach a body.
	/// </summary>
	/// <param name="routes">The airport's transitions and bodies.</param>
	/// <returns>The paths, as keyed coordinates.</returns>
	internal static List<IReadOnlyList<(string Key, Coordinate Coordinate)>> Paths(IReadOnlyList<ArrivalRoute> routes)
	{
		List<ArrivalRoute> bodies = [.. routes.Where(r => r.Kind == ArrivalRouteKind.Body && r.Points.Count > 0)];
		List<ArrivalRoute> transitions = [.. routes.Where(r => r.Kind == ArrivalRouteKind.Transition && r.Points.Count > 0)];

		List<IReadOnlyList<(string Key, Coordinate Coordinate)>> paths = [];
		HashSet<ArrivalRoute> usedTransitions = new(ReferenceEqualityComparer.Instance);

		foreach (ArrivalRoute body in bodies)
		{
			string firstId = body.Points[0].Id;
			List<ArrivalRoute> reaching = [.. transitions.Where(t => string.Equals(t.Points[^1].Id, firstId, StringComparison.OrdinalIgnoreCase))];

			if (reaching.Count == 0)
			{
				paths.Add(ToPath(body.Points));
				continue;
			}

			foreach (ArrivalRoute transition in reaching)
			{
				usedTransitions.Add(transition);
				paths.Add(ToPath(transition.Points.Concat(body.Points.Skip(1))));
			}
		}

		foreach (ArrivalRoute transition in transitions.Where(t => !usedTransitions.Contains(t)))
		{
			paths.Add(ToPath(transition.Points));
		}

		return paths;
	}

	private static IReadOnlyList<(string Key, Coordinate Coordinate)> ToPath(IEnumerable<ArrivalPoint> points) =>
		points.Select(p => (p.Id, new Coordinate(p.Longitude, p.Latitude))).ToList();
}

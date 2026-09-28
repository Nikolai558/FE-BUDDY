using NetTopologySuite.Geometries;

using Location = FeBuddy.Core.Domain.Geo.Models.Location;

namespace FeBuddy.Core.Domain.Geo;

/// <summary>
/// Clips lines to a circle: everything within a distance of a centre point is kept, and a
/// segment that crosses the circle is cut exactly where it meets it.
/// </summary>
/// <remarks>
/// <para>
/// Each segment is treated as the great-circle arc between its ends, on the same sphere as
/// <see cref="GeoMath"/>. Along that arc the distance to the centre has a closed form, so the
/// part inside the circle is solved for directly rather than searched for - which also catches a
/// segment whose ends are both outside but whose middle passes through the circle.
/// </para>
/// <para>
/// The circle's radius must be well under a quarter of the Earth's circumference (about
/// 5,400 NM) and every segment shorter than half of it; FAA video maps are nowhere near either.
/// </para>
/// </remarks>
public static class RadiusFilter
{
	/// <summary>Angles closer than this (radians, about 6 mm on the ground) are the same point on an arc.</summary>
	private const double Epsilon = 1e-9;

	/// <summary>
	/// Clips lines to the circle of <paramref name="radiusNm"/> around <paramref name="center"/>.
	/// </summary>
	/// <param name="lines">The lines to clip.</param>
	/// <param name="center">The circle's centre.</param>
	/// <param name="radiusNm">The circle's radius, in nautical miles. Must be greater than zero.</param>
	/// <returns>
	/// The pieces inside the circle, in the order they occur. A line that leaves and re-enters
	/// the circle becomes one piece per visit; a line wholly outside contributes nothing.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radiusNm"/> is not greater than zero.</exception>
	public static IReadOnlyList<LineString> ClipLines(IReadOnlyList<LineString> lines, Location center, double radiusNm)
	{
		ArgumentNullException.ThrowIfNull(lines);
		ArgumentNullException.ThrowIfNull(center);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusNm);

		Vector centre = Vector.From(center.DecLat, center.DecLon);

		// A point is inside when the angle between it and the centre is at most the radius's
		// angle - i.e. when their dot product is at least its cosine.
		double minimumDot = Math.Cos(radiusNm * GeoMath.MetresPerNauticalMile / GeoMath.EarthRadiusMetres);

		List<LineString> clipped = [];

		foreach (LineString line in lines)
		{
			ClipLine(line.Coordinates, centre, minimumDot, clipped);
		}

		return clipped;
	}

	private static void ClipLine(Coordinate[] coordinates, Vector centre, double minimumDot, List<LineString> clipped)
	{
		List<Coordinate> current = [];

		for (int i = 1; i < coordinates.Length; i++)
		{
			Coordinate start = coordinates[i - 1];
			Coordinate end = coordinates[i];
			Vector a = Vector.From(start.Y, start.X);
			Vector b = Vector.From(end.Y, end.X);
			double arc = Math.Acos(Math.Clamp(a.Dot(b), -1, 1));

			// A repeated point draws nothing, and must not break the line it sits in.
			if (arc < Epsilon)
			{
				continue;
			}

			if (InsideSpan(a, b, arc, centre, minimumDot) is not { } span)
			{
				Flush(current, clipped);
				continue;
			}

			// A piece that starts part-way along the segment has just entered the circle, so it
			// cannot continue whatever was being built before it.
			if (!span.StartsAtStart)
			{
				Flush(current, clipped);
			}

			if (current.Count == 0)
			{
				current.Add(span.StartsAtStart ? start.Copy() : span.Start);
			}

			current.Add(span.EndsAtEnd ? end.Copy() : span.End);

			if (!span.EndsAtEnd)
			{
				Flush(current, clipped);
			}
		}

		Flush(current, clipped);
	}

	/// <summary>
	/// The part of the arc from <paramref name="a"/> to <paramref name="b"/>, <paramref name="arc"/>
	/// radians long, that lies inside the circle, or <see langword="null"/> when none of it does.
	/// </summary>
	private static Span? InsideSpan(Vector a, Vector b, double arc, Vector centre, double minimumDot)
	{
		// Walk the arc as P(t) = a cos t + u sin t, for t from 0 to the arc's angle, where u is the
		// unit vector at right angles to a in the plane of a and b. Then centre . P(t) is
		// amplitude * cos(t - phase), and "inside" is that being at least minimumDot.
		Vector u = b.Minus(a.Times(a.Dot(b))).Normalized();
		double alongA = centre.Dot(a);
		double alongU = centre.Dot(u);
		double amplitude = Math.Sqrt(alongA * alongA + alongU * alongU);

		if (amplitude < minimumDot)
		{
			return null;
		}

		double phase = Math.Atan2(alongU, alongA);
		double halfWidth = Math.Acos(Math.Clamp(minimumDot / amplitude, -1, 1));

		// The inside stretch is phase ± halfWidth, give or take a whole turn; with the radius and
		// the segment both under half a turn, at most one of those lands on this segment.
		for (int turn = -1; turn <= 1; turn++)
		{
			double from = Math.Max(0, phase - halfWidth + turn * 2 * Math.PI);
			double to = Math.Min(arc, phase + halfWidth + turn * 2 * Math.PI);

			if (to - from < Epsilon)
			{
				continue;
			}

			return new Span(
				StartsAtStart: from < Epsilon,
				EndsAtEnd: arc - to < Epsilon,
				Start: a.Times(Math.Cos(from)).Plus(u.Times(Math.Sin(from))).ToCoordinate(),
				End: a.Times(Math.Cos(to)).Plus(u.Times(Math.Sin(to))).ToCoordinate());
		}

		return null;
	}

	private static void Flush(List<Coordinate> current, List<LineString> clipped)
	{
		if (current.Count >= 2)
		{
			clipped.Add(Wgs84.Factory.CreateLineString([.. current]));
		}

		current.Clear();
	}

	/// <summary>The inside part of one segment, and whether it reaches either end.</summary>
	private readonly record struct Span(bool StartsAtStart, bool EndsAtEnd, Coordinate Start, Coordinate End);

	/// <summary>A point on the unit sphere, as an Earth-centred vector.</summary>
	private readonly record struct Vector(double X, double Y, double Z)
	{
		public static Vector From(double latitude, double longitude)
		{
			double lat = latitude * Math.PI / 180;
			double lon = longitude * Math.PI / 180;
			return new Vector(Math.Cos(lat) * Math.Cos(lon), Math.Cos(lat) * Math.Sin(lon), Math.Sin(lat));
		}

		public double Dot(Vector other) => X * other.X + Y * other.Y + Z * other.Z;

		public Vector Plus(Vector other) => new(X + other.X, Y + other.Y, Z + other.Z);

		public Vector Minus(Vector other) => new(X - other.X, Y - other.Y, Z - other.Z);

		public Vector Times(double factor) => new(X * factor, Y * factor, Z * factor);

		public Vector Normalized() => Times(1 / Math.Sqrt(Dot(this)));

		/// <summary>Back to a longitude / latitude coordinate (NTS order: X = longitude).</summary>
		public Coordinate ToCoordinate() => new(
			Math.Atan2(Y, X) * 180 / Math.PI,
			Math.Asin(Math.Clamp(Z, -1, 1)) * 180 / Math.PI);
	}
}

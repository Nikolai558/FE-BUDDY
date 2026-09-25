using NetTopologySuite.Geometries;

namespace FeBuddy.Core.Infrastructure.Sct.Models;

/// <summary>One <c>[LABELS]</c> entry: text drawn at a point.</summary>
/// <param name="Text">The label, without its quotes.</param>
/// <param name="Position">Where it is drawn (NTS order: X = longitude, Y = latitude).</param>
public sealed record SctLabel(string Text, Coordinate Position);

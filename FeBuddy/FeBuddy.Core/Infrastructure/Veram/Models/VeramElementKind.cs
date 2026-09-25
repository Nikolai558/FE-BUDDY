namespace FeBuddy.Core.Infrastructure.Veram.Models;

/// <summary>What a vERAM GeoMap <c>Element</c> draws, from its <c>xsi:type</c>.</summary>
public enum VeramElementKind
{
	/// <summary>A line segment (<c>xsi:type="Line"</c>).</summary>
	Line,

	/// <summary>A symbol at a point (<c>xsi:type="Symbol"</c>).</summary>
	Symbol,

	/// <summary>Text at a point (<c>xsi:type="Text"</c>).</summary>
	Text,
}

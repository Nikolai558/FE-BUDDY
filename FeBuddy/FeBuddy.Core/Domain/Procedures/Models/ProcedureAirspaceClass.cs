namespace FeBuddy.Core.Domain.Procedures.Models;

/// <summary>
/// The highest class airspace (NASR <c>CLS_ARSP</c>) overlying an airport, ranked
/// <see cref="B"/> &gt; <see cref="C"/> &gt; <see cref="D"/> &gt; <see cref="E"/>.
/// </summary>
/// <remarks>
/// Member names double as the JSON <c>airspaceClass</c> value (<see cref="object.ToString"/>), so
/// they are kept to the bare letter. Declaration order also doubles as rank, lowest first, so
/// picking the highest flagged class is a plain <c>Enumerable.Max</c>.
/// </remarks>
public enum ProcedureAirspaceClass
{
	/// <summary>No <c>CLS_ARSP</c> row for the airport flags any class.</summary>
	None,

	/// <summary>Class Echo.</summary>
	E,

	/// <summary>Class Delta.</summary>
	D,

	/// <summary>Class Charlie.</summary>
	C,

	/// <summary>Class Bravo.</summary>
	B,
}

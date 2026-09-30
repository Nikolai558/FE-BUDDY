namespace FeBuddy.Wpf.Map.Models;

/// <summary>One of the background reference layers (<see cref="BaseMap"/>), each switched on separately.</summary>
public enum BaseMapLayer
{
	/// <summary>Every US state and territory, whole: state lines and the US coast.</summary>
	UsStates,

	/// <summary>The world's coastlines and its largest lakes.</summary>
	Coastlines,
}

namespace FeBuddy.Core.Application.Airac.Navaids.Models;

/// <summary>
/// The FE-Buddy custom properties a NAVAID Feature can carry. The user picks which ones they
/// want; each selected value is written as <c>"feb.&lt;name&gt;"</c>.
/// </summary>
/// <remarks>
/// <para>
/// There is deliberately no latitude or longitude property: every Feature's geometry already
/// carries its coordinates. The same rule applies to every sub-service's <c>feb.*</c> list.
/// </para>
/// <para>
/// <see cref="NavId"/>, <see cref="NavType"/> and <see cref="Name"/> are never written to the
/// Text file: its <c>text</c> array already carries all three, so repeating them would only
/// inflate the file.
/// </para>
/// </remarks>
public enum NavaidFebProperty
{
	/// <summary><c>feb.navId</c></summary>
	NavId = 0,

	/// <summary><c>feb.navType</c></summary>
	NavType = 1,

	/// <summary><c>feb.name</c></summary>
	Name = 2,

	/// <summary>
	/// <c>feb.freq</c> - the NASR frequency as published (a number), omitted when the NAVAID
	/// has none. Unlike the alias file, this is never reformatted for display.
	/// </summary>
	Freq = 3,

	/// <summary>
	/// <c>feb.lowAltArtccId</c> - omitted when the NAVAID publishes no low altitude boundary
	/// ARTCC.
	/// </summary>
	LowAltArtccId = 4,

	/// <summary>
	/// <c>feb.highAltArtccId</c> - omitted when the NAVAID publishes no high altitude boundary
	/// ARTCC.
	/// </summary>
	HighAltArtccId = 5,
}

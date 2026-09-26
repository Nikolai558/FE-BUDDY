namespace FeBuddy.Core.Application.Airac.Fixes.Models;

/// <summary>
/// The FE-Buddy custom properties a fix Feature can carry. The user picks which ones they want;
/// each selected value is written as <c>"feb.&lt;name&gt;"</c>.
/// </summary>
/// <remarks>
/// There is deliberately no latitude or longitude property: every Feature's geometry already
/// carries its coordinates. <see cref="FixId"/> is never written to the Text file: its
/// <c>text</c> array already carries it.
/// </remarks>
public enum FixFebProperty
{
	/// <summary><c>feb.fixId</c></summary>
	FixId = 0,

	/// <summary><c>feb.fixUseCode</c> - the fix's mapped type of use, e.g. <c>WYPNT</c>.</summary>
	FixUseCode = 1,

	/// <summary><c>feb.charts</c> - the NASR chart names the fix is depicted on, omitted when it has none.</summary>
	Charts = 2,
}

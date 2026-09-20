namespace FeBuddy.Core.Models.Services.Airac.Airports;

/// <summary>
/// The FE-Buddy custom properties an airport Feature can carry. The user picks which ones they
/// want; each selected value is written as <c>"feb.&lt;name&gt;"</c>.
/// </summary>
/// <remarks>
/// <see cref="FaaId"/> and <see cref="Name"/> are never written to the Text file: its
/// <c>text</c> array already carries both, so repeating them would only inflate the file.
/// </remarks>
public enum AirportFebProperty
{
	/// <summary><c>feb.faaId</c></summary>
	FaaId = 0,

	/// <summary><c>feb.icaoId</c></summary>
	IcaoId = 1,

	/// <summary><c>feb.name</c></summary>
	Name = 2,

	/// <summary><c>feb.lat</c></summary>
	Lat = 3,

	/// <summary><c>feb.lon</c></summary>
	Lon = 4,

	/// <summary><c>feb.elev</c></summary>
	Elev = 5,

	/// <summary><c>feb.respArtcc</c></summary>
	RespArtcc = 6,

	/// <summary><c>feb.tfcPtrnAlt</c></summary>
	TfcPtrnAlt = 7,

	/// <summary><c>feb.fssId</c></summary>
	FssId = 8,

	/// <summary><c>feb.twrType</c></summary>
	TwrType = 9,
}

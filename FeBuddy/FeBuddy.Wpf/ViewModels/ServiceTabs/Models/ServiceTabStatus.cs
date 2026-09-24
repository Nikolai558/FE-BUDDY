namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// How a tab is presented in the service tab rail.
/// </summary>
public enum ServiceTabStatus
{
	/// <summary>Saved, complete and valid - the rail shows it plain.</summary>
	Ok,

	/// <summary>The user changed something and has not saved it - the rail warns (amber).</summary>
	Unsaved,

	/// <summary>A required value is missing or a value is invalid - the rail alerts (red).</summary>
	Invalid,
}

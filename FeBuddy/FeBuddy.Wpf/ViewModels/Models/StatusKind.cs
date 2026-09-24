namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Visual state for a systems-health / status row.</summary>
public enum StatusKind
{
	/// <summary>Working normally (green).</summary>
	Ok,

	/// <summary>Working, but needs a look or is still settling (amber).</summary>
	Warn,

	/// <summary>Not working (red).</summary>
	Down,
}

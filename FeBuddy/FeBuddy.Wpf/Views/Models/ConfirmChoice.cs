namespace FeBuddy.Wpf.Views.Models;

/// <summary>Which button closed a <see cref="ConfirmWindow"/>.</summary>
public enum ConfirmChoice
{
	/// <summary>Cancel, or the window was closed.</summary>
	Cancel = 0,

	/// <summary>The confirm (primary) button.</summary>
	Confirm = 1,

	/// <summary>The alternative button, when the dialog offers one.</summary>
	Alternative = 2,
}

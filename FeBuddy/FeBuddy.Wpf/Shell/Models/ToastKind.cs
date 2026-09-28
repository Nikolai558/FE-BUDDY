namespace FeBuddy.Wpf.Shell.Models;

/// <summary>A toast's severity, which sets its colour and icon.</summary>
public enum ToastKind
{
	/// <summary>Neutral information.</summary>
	Info,

	/// <summary>Something finished successfully.</summary>
	Success,

	/// <summary>Something needs the user's attention.</summary>
	Warn,

	/// <summary>Something failed.</summary>
	Error,
}

namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>The setup window's pages, in the order a fresh install visits them.</summary>
public enum SetupPage
{
	/// <summary>What this run will do: install, update, go back, repair or remove - or why it can't.</summary>
	Welcome,

	/// <summary>The GNU GPL v3 (fresh installs only).</summary>
	License,

	/// <summary>Install folder and shortcuts (fresh installs only).</summary>
	Options,

	/// <summary>Windows Installer at work.</summary>
	Progress,

	/// <summary>How it ended.</summary>
	Finish,
}

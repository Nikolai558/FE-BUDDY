using System.Collections.Generic;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.ViewModels;

/// <summary>The kind of a prompt, which picks its icon and colour.</summary>
public enum PromptKind
{
	/// <summary>Something went wrong (red).</summary>
	Error,

	/// <summary>Something needs attention (amber).</summary>
	Warning,

	/// <summary>Information, or a question (neutral amber accent).</summary>
	Info,
}

/// <summary>One button of a prompt.</summary>
/// <param name="Label">Its text.</param>
/// <param name="Result">What Windows Installer is told when it is chosen.</param>
/// <param name="IsPrimary">The amber main action (and the default).</param>
/// <param name="IsDanger">Red: an action that stops or removes something.</param>
public sealed record PromptButton(string Label, MessageResult Result, bool IsPrimary = false, bool IsDanger = false);

/// <summary>
/// A question for the user in the window's own style - in place of the message boxes Windows
/// Installer would show (an error, a warning, files in use).
/// </summary>
/// <param name="Kind">Its icon and colour.</param>
/// <param name="Title">The heading.</param>
/// <param name="Message">The text.</param>
/// <param name="Items">A list under the text (the applications holding files), or none.</param>
/// <param name="Buttons">The choices, left to right.</param>
/// <param name="CloseResult">What closing the prompt without choosing means.</param>
public sealed record PromptRequest(
	PromptKind Kind,
	string Title,
	string Message,
	IReadOnlyList<string> Items,
	IReadOnlyList<PromptButton> Buttons,
	MessageResult CloseResult);

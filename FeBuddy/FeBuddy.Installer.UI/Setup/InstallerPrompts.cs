using System.Collections.Generic;
using System.Linq;

using FeBuddy.Installer.UI.ViewModels;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.Setup;

/// <summary>
/// Windows Installer's questions, as prompts for the setup window: the same buttons, answering with
/// the same results, as the message boxes it would show itself.
/// </summary>
public static class InstallerPrompts
{
	private const string Product = SetupContext.ProductName;

	/// <summary>An error, warning, user or out-of-disk-space message.</summary>
	/// <param name="type">The message type.</param>
	/// <param name="text">The formatted message.</param>
	/// <param name="buttons">The buttons Windows Installer asked for.</param>
	/// <param name="icon">The icon it asked for.</param>
	/// <returns>The prompt.</returns>
	public static PromptRequest ForMessage(InstallMessage type, string text, MessageButtons buttons, MessageIcon icon)
	{
		PromptKind kind = type switch
		{
			InstallMessage.Error or InstallMessage.OutOfDiskSpace => PromptKind.Error,
			InstallMessage.Warning => PromptKind.Warning,
			_ => icon switch
			{
				MessageIcon.Error or MessageIcon.Hand or MessageIcon.Stop => PromptKind.Error,
				MessageIcon.Warning or MessageIcon.Exclamation => PromptKind.Warning,
				_ => PromptKind.Info,
			},
		};

		string title = type == InstallMessage.OutOfDiskSpace
			? "There isn't enough disk space"
			: kind switch
			{
				PromptKind.Error => "Setup ran into a problem",
				PromptKind.Warning => "Setup needs your attention",
				_ => $"{Product} Setup",
			};

		(IReadOnlyList<PromptButton> choices, MessageResult close) = Buttons(buttons);
		return new PromptRequest(kind, title, text.Trim(), [], choices, close);
	}

	/// <summary>Applications holding files Setup has to replace.</summary>
	/// <param name="applications">Their names.</param>
	/// <param name="restartManager">
	/// The Restart Manager's form (<c>INSTALLMESSAGE_RMFILESINUSE</c>): Windows Installer can close
	/// the applications itself (OK). Otherwise the older form: the user closes them and retries.
	/// </param>
	/// <returns>The prompt.</returns>
	public static PromptRequest ForFilesInUse(IReadOnlyList<string> applications, bool restartManager)
	{
		const string Title = "Some applications need to close";

		if (restartManager)
		{
			return new PromptRequest(
				PromptKind.Warning,
				Title,
				"These are using files Setup needs to update. Setup can close them now and carry on - save any work in them first.",
				applications,
				[
					new PromptButton("Cancel setup", MessageResult.Cancel, IsDanger: true),
					new PromptButton("Don't close", MessageResult.Ignore),
					new PromptButton("Close and continue", MessageResult.OK, IsPrimary: true),
				],
				MessageResult.Cancel);
		}

		return new PromptRequest(
			PromptKind.Warning,
			Title,
			"These are using files Setup needs to update. Close them, then choose Try again.",
			applications,
			[
				new PromptButton("Cancel setup", MessageResult.Cancel, IsDanger: true),
				new PromptButton("Continue anyway", MessageResult.Ignore),
				new PromptButton("Try again", MessageResult.Retry, IsPrimary: true),
			],
			MessageResult.Cancel);
	}

	/// <summary>
	/// The applications named in a files-in-use record. Fields come in pairs after field 0: a file
	/// or process id, then the window title or application name - the title is what a user knows.
	/// </summary>
	/// <param name="record">The record.</param>
	/// <returns>Distinct application names.</returns>
	public static IReadOnlyList<string> Applications(Record record)
	{
		List<string> names = [];
		for (int field = 1; field <= record.FieldCount; field += 2)
		{
			string first = record.IsNull(field) ? string.Empty : record.GetString(field);
			string second = field + 1 <= record.FieldCount && !record.IsNull(field + 1) ? record.GetString(field + 1) : string.Empty;
			string name = !string.IsNullOrWhiteSpace(second) && !second.All(char.IsDigit) ? second : first;
			if (!string.IsNullOrWhiteSpace(name) && !name.All(char.IsDigit))
			{
				names.Add(name.Trim());
			}
		}

		return [.. names.Distinct()];
	}

	// The buttons for a MessageBox-style button set, and what closing the prompt means. Windows
	// Installer labels Abort "Cancel" in its own boxes, and so does this.
	private static (IReadOnlyList<PromptButton> Buttons, MessageResult Close) Buttons(MessageButtons buttons) => buttons switch
	{
		MessageButtons.OKCancel => (
			[new PromptButton("Cancel", MessageResult.Cancel), new PromptButton("OK", MessageResult.OK, IsPrimary: true)],
			MessageResult.Cancel),
		MessageButtons.AbortRetryIgnore => (
			[
				new PromptButton("Cancel setup", MessageResult.Abort, IsDanger: true),
				new PromptButton("Ignore", MessageResult.Ignore),
				new PromptButton("Try again", MessageResult.Retry, IsPrimary: true),
			],
			MessageResult.Abort),
		MessageButtons.YesNoCancel => (
			[
				new PromptButton("Cancel", MessageResult.Cancel),
				new PromptButton("No", MessageResult.No),
				new PromptButton("Yes", MessageResult.Yes, IsPrimary: true),
			],
			MessageResult.Cancel),
		MessageButtons.YesNo => (
			[new PromptButton("No", MessageResult.No), new PromptButton("Yes", MessageResult.Yes, IsPrimary: true)],
			MessageResult.No),
		MessageButtons.RetryCancel => (
			[new PromptButton("Cancel setup", MessageResult.Cancel, IsDanger: true), new PromptButton("Try again", MessageResult.Retry, IsPrimary: true)],
			MessageResult.Cancel),
		_ => (
			[new PromptButton("OK", MessageResult.OK, IsPrimary: true)],
			MessageResult.OK),
	};
}

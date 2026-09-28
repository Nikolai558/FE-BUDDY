using System.Collections.ObjectModel;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One entry in a credential drop-down. Holds the credential's id and name only - never its secret.</summary>
/// <param name="Id">The credential's id; <see cref="Guid.Empty"/> for "no credential".</param>
/// <param name="Name">The credential's name.</param>
/// <param name="Label">What the drop-down shows, e.g. <c>ZOB GitHub (GitHub personal access token)</c>.</param>
public sealed record CredentialChoice(Guid Id, string Name, string Label)
{
	/// <summary>Download without a credential.</summary>
	public static CredentialChoice None { get; } = new(Guid.Empty, "None", "None - the file is public");

	/// <summary>The drop-down entry for a saved credential.</summary>
	/// <param name="info">The credential.</param>
	/// <returns>The entry.</returns>
	public static CredentialChoice For(CredentialInfo info)
	{
		ArgumentNullException.ThrowIfNull(info);

		return new CredentialChoice(info.Id, info.Name, $"{info.Name} ({info.Kind.DisplayName()})");
	}

	/// <summary>
	/// Brings a drop-down's entries up to date without clearing it, so a chosen credential stays
	/// chosen while the list changes around it (clearing would make the drop-down drop its choice).
	/// </summary>
	/// <param name="choices">The drop-down's entries.</param>
	/// <param name="wanted">What they should be, in order.</param>
	public static void Sync(ObservableCollection<CredentialChoice> choices, IReadOnlyList<CredentialChoice> wanted)
	{
		ArgumentNullException.ThrowIfNull(choices);
		ArgumentNullException.ThrowIfNull(wanted);

		for (int i = choices.Count - 1; i >= 0; i--)
		{
			if (!wanted.Contains(choices[i]))
			{
				choices.RemoveAt(i);
			}
		}

		for (int i = 0; i < wanted.Count; i++)
		{
			if (i < choices.Count && choices[i] == wanted[i])
			{
				continue;
			}

			int existing = choices.IndexOf(wanted[i]);

			if (existing >= 0)
			{
				choices.Move(existing, i);
			}
			else
			{
				choices.Insert(i, wanted[i]);
			}
		}
	}
}

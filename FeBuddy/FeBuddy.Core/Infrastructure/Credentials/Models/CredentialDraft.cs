namespace FeBuddy.Core.Infrastructure.Credentials.Models;

/// <summary>A credential being added or edited, before <see cref="CredentialStore.Save(CredentialDraft)"/> stores it.</summary>
/// <param name="Id">The credential being edited, or <see langword="null"/> for a new one.</param>
/// <param name="Name">The name the user gives it.</param>
/// <param name="Kind">What it is.</param>
/// <param name="UserName">The user name, for <see cref="CredentialKind.UsernamePassword"/>; ignored otherwise.</param>
/// <param name="Secret">
/// The password or token. When editing, blank keeps the saved one - the saved secret is never shown
/// again, so the user only types it to replace it.
/// </param>
/// <param name="Hosts">The websites it may be sent to.</param>
/// <param name="UseForFeBuddyGitHub">Whether FE-Buddy's own GitHub requests use it; only a <see cref="CredentialKind.GitHubToken"/> can.</param>
public sealed record CredentialDraft(
	Guid? Id,
	string Name,
	CredentialKind Kind,
	string? UserName,
	string? Secret,
	IReadOnlyList<string> Hosts,
	bool UseForFeBuddyGitHub = false);

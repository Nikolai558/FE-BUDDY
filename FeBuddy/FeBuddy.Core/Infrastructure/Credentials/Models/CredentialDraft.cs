using System.Globalization;
using System.Text;

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
public sealed record CredentialDraft(
	Guid? Id,
	string Name,
	CredentialKind Kind,
	string? UserName,
	string? Secret,
	IReadOnlyList<string> Hosts)
{
	/// <summary>
	/// What the draft's <see cref="object.ToString"/> prints: every member but the secret, so a log
	/// line, a test failure or a debugger tooltip never shows it.
	/// </summary>
	private bool PrintMembers(StringBuilder builder)
	{
		string secret = Secret is null ? "null" : "(hidden)";
		string hosts = Hosts is null ? "null" : $"[{string.Join(", ", Hosts)}]";

		builder.Append(CultureInfo.InvariantCulture,
			$"Id = {Id}, Name = {Name}, Kind = {Kind}, UserName = {UserName}, Secret = {secret}, Hosts = {hosts}");
		return true;
	}
}

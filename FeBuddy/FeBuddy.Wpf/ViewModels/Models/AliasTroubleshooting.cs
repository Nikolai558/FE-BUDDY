namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// What a custom alias file's row offers after GitHub refused or hid the file on <b>Check</b>: a
/// question first, then the help that fits the answer.
/// </summary>
public enum AliasTroubleshooting
{
	/// <summary>Nothing to offer: the file was read, or the problem was something else.</summary>
	None,

	/// <summary>No credential is chosen: is the repository private?</summary>
	AskIfPrivate,

	/// <summary>The repository is private: it needs a GitHub token that can read it.</summary>
	Private,

	/// <summary>The repository is public: the address is most likely wrong.</summary>
	Public,

	/// <summary>A credential is chosen, and GitHub refused it or it can't see the file.</summary>
	CredentialRefused,
}
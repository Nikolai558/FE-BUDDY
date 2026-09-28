using System.ComponentModel;

using FeBuddy.Core.Infrastructure.Credentials;

namespace FeBuddy.UnitTests.TestSupport;

/// <summary>
/// A test that needs the real Windows Credential Manager. A session with no Windows sign-in behind
/// it - a service account, some build agents - has none, so there the test is reported as skipped,
/// with the reason: not failed, since that is the machine and not the code, and not passed, since
/// nothing was tested.
/// </summary>
public sealed class CredentialManagerFactAttribute : FactAttribute
{
	/// <summary>ERROR_NO_SUCH_LOGON_SESSION: what Credential Manager answers when there is none.</summary>
	private const int NoLogonSession = 1312;

	private static readonly Lazy<string?> WhyUnavailable = new(Probe);

	/// <summary>Skips the test when this session has no Credential Manager.</summary>
	public CredentialManagerFactAttribute()
	{
		if (WhyUnavailable.Value is { } reason)
		{
			Skip = reason;
		}
	}

	/// <summary>Why Credential Manager cannot be used in this session, or <see langword="null"/> when it can.</summary>
	private static string? Probe()
	{
		try
		{
			// A name nothing uses: a working Credential Manager answers "not found".
			new WindowsCredentialVault().Read($"FE-Buddy-Tests:probe:{Guid.NewGuid():N}");
			return null;
		}
		catch (Win32Exception ex) when (ex.NativeErrorCode == NoLogonSession)
		{
			return "This session has no Windows Credential Manager (no Windows sign-in behind it).";
		}
	}
}

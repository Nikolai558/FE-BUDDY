using System.ComponentModel;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.UnitTests.Infrastructure.Credentials;

/// <summary>
/// Exercises <see cref="WindowsCredentialVault"/> against the real Windows Credential Manager. Every
/// entry these tests write is named <c>FE-Buddy-Tests:&lt;guid&gt;:…</c> - never FE-Buddy's own
/// prefix - and is removed afterwards, so the user's real credentials are never touched.
/// </summary>
/// <remarks>
/// A session with no Windows sign-in behind it (some services and build agents) has no Credential
/// Manager: every test that calls it is then reported as skipped (<see cref="CredentialManagerFactAttribute"/>).
/// </remarks>
public sealed class WindowsCredentialVaultTests : IDisposable
{
	private readonly WindowsCredentialVault _vault = new();
	private readonly string _prefix = $"FE-Buddy-Tests:{Guid.NewGuid():N}:";

	/// <summary>Removes every entry this test wrote.</summary>
	public void Dispose()
	{
		try
		{
			foreach (VaultEntry entry in _vault.Enumerate(_prefix))
			{
				_vault.Delete(entry.Target);
			}
		}
		catch (Win32Exception)
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>An entry written can be read, listed and removed; its bytes come back exactly.</summary>
	[CredentialManagerFact]
	public void write_read_enumerate_delete_round_trip()
	{
		byte[] secret = [0, 1, 2, 250, 255];

		_vault.Write(_prefix + "one", "Test user", secret);
		_vault.Write(_prefix + "two", "Test user", []);

		Assert.Equal(secret, _vault.Read(_prefix + "one"));
		Assert.Empty(_vault.Read(_prefix + "two")!);

		IReadOnlyList<VaultEntry> listed = _vault.Enumerate(_prefix);
		Assert.Equal(2, listed.Count);
		Assert.Equal(secret, listed.Single(e => e.Target == _prefix + "one").Secret);

		Assert.True(_vault.Delete(_prefix + "one"));
		Assert.False(_vault.Delete(_prefix + "one"));
		Assert.Null(_vault.Read(_prefix + "one"));
	}

	/// <summary>Writing again replaces the entry.</summary>
	[CredentialManagerFact]
	public void write_replaces()
	{
		_vault.Write(_prefix + "entry", "Test user", [1]);
		_vault.Write(_prefix + "entry", "Test user", [2, 2]);

		Assert.Equal([2, 2], _vault.Read(_prefix + "entry"));
	}

	/// <summary>Listing matches the exact prefix only, and nothing is an empty list.</summary>
	[CredentialManagerFact]
	public void enumerate_matches_the_exact_prefix()
	{
		Assert.Empty(_vault.Enumerate(_prefix));

		_vault.Write(_prefix + "entry", "Test user", [1]);

		Assert.Empty(_vault.Enumerate(_prefix.ToUpperInvariant()));
		Assert.Single(_vault.Enumerate(_prefix));
	}

	/// <summary>A missing entry reads as nothing.</summary>
	[CredentialManagerFact]
	public void read_missing_is_null() =>
		Assert.Null(_vault.Read(_prefix + "missing"));

	/// <summary>More than Credential Manager can hold is refused before Windows is asked.</summary>
	[Fact]
	public void write_too_large_throws() =>
		Assert.Throws<ArgumentException>(() => _vault.Write(_prefix + "big", "Test user", new byte[WindowsCredentialVault.MaxSecretBytes + 1]));

	/// <summary>Blank names and null values are programming errors.</summary>
	[Fact]
	public void arguments_are_checked()
	{
		Assert.Throws<ArgumentException>(() => _vault.Write("", "u", []));
		Assert.Throws<ArgumentNullException>(() => _vault.Write(_prefix + "x", null!, []));
		Assert.Throws<ArgumentNullException>(() => _vault.Write(_prefix + "x", "u", null!));
		Assert.Throws<ArgumentException>(() => _vault.Read(""));
		Assert.Throws<ArgumentException>(() => _vault.Delete(""));
		Assert.Throws<ArgumentException>(() => _vault.Enumerate(""));
	}

	/// <summary>A target Windows refuses (too long) surfaces as a Windows error.</summary>
	[CredentialManagerFact]
	public void windows_errors_surface()
	{
		string tooLong = _prefix + new string('x', 40_000);

		Assert.Throws<Win32Exception>(() => _vault.Write(tooLong, "Test user", [1]));
		Assert.Throws<Win32Exception>(() => _vault.Read(tooLong));
		Assert.Throws<Win32Exception>(() => _vault.Delete(tooLong));
	}
}

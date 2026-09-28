using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.UnitTests.TestSupport;

/// <summary>An <see cref="ICredentialVault"/> kept in memory, so credential tests never touch the real Credential Manager.</summary>
internal sealed class InMemoryCredentialVault : ICredentialVault
{
	private readonly Dictionary<string, (string UserName, byte[] Secret)> _entries = new(StringComparer.Ordinal);

	/// <summary>The user-name field each entry was written with, by target.</summary>
	public IReadOnlyDictionary<string, string> UserNames => _entries.ToDictionary(e => e.Key, e => e.Value.UserName, StringComparer.Ordinal);

	/// <summary>Every entry's raw bytes, by target, as a real vault would hold them.</summary>
	public IReadOnlyDictionary<string, byte[]> Raw => _entries.ToDictionary(e => e.Key, e => e.Value.Secret, StringComparer.Ordinal);

	/// <summary>When set, reading or listing entries throws it, as a Credential Manager that cannot be read would.</summary>
	public Exception? Failure { get; set; }

	/// <summary>The buffer last passed to <see cref="Write"/> - the caller's own, not the vault's copy.</summary>
	public byte[]? LastWriteBuffer { get; private set; }

	public void Write(string target, string userName, byte[] secret)
	{
		LastWriteBuffer = secret;
		_entries[target] = (userName, [.. secret]);
	}

	public byte[]? Read(string target) =>
		Failure is not null ? throw Failure
		: _entries.TryGetValue(target, out (string UserName, byte[] Secret) entry) ? [.. entry.Secret]
		: null;

	public bool Delete(string target) => _entries.Remove(target);

	public IReadOnlyList<VaultEntry> Enumerate(string prefix) =>
		Failure is not null
			? throw Failure
			: [.. _entries.Where(e => e.Key.StartsWith(prefix, StringComparison.Ordinal)).Select(e => new VaultEntry(e.Key, [.. e.Value.Secret]))];

	/// <summary>Plants raw bytes under a target, e.g. a damaged entry.</summary>
	public void Plant(string target, byte[] bytes) => _entries[target] = ("planted", bytes);
}

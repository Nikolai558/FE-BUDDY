namespace FeBuddy.Core.Infrastructure.Credentials.Models;

/// <summary>One entry read back from an <see cref="ICredentialVault"/>.</summary>
/// <param name="Target">The entry's name in the vault, e.g. <c>FE-Buddy:credential:…</c>.</param>
/// <param name="Secret">The bytes stored with it.</param>
public sealed record VaultEntry(string Target, byte[] Secret);

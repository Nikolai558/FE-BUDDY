using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Core.Infrastructure.GitHub.Models;

/// <summary>What <see cref="GitHubAuth.MoveLegacyToken()"/> did.</summary>
/// <param name="Credential">The GitHub credential the token now lives in.</param>
/// <param name="MachineVariableRemains">Whether a machine-wide copy of the variable is left, for an administrator to remove.</param>
public sealed record LegacyTokenMove(CredentialInfo Credential, bool MachineVariableRemains);

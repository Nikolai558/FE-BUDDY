namespace FeBuddy.Core.Infrastructure.FileSystem.Models;

/// <summary>What a reset asked for with <see cref="AppDataReset.Request"/> does at the next launch.</summary>
/// <param name="KeepSettings">Whether the settings (every profile) are kept; everything else FE-Buddy stores is deleted either way.</param>
/// <param name="DeleteCredentials">Whether every FE-Buddy credential is removed from Windows Credential Manager.</param>
/// <param name="WaitForProcessId">
/// The FE-Buddy that asked for the reset, which the next launch waits to close before it deletes
/// anything; <see langword="null"/> for none.
/// </param>
public sealed record AppDataResetRequest(bool KeepSettings, bool DeleteCredentials, int? WaitForProcessId = null);

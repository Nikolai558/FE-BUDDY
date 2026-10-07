namespace FeBuddy.Core.Infrastructure.FileSystem.Models;

/// <summary>What <see cref="AppDataReset.RunPending"/> did.</summary>
/// <param name="SettingsKept">Whether the settings (every profile) were kept.</param>
/// <param name="CredentialsRemoved">
/// How many credentials were removed from Windows Credential Manager; <see langword="null"/> when
/// the reset kept them.
/// </param>
/// <param name="NotDeleted">
/// The files and folders under <c>%APPDATA%\FE-Buddy</c> that could not be deleted (in use, say),
/// by name. Empty when everything went.
/// </param>
public sealed record AppDataResetResult(bool SettingsKept, int? CredentialsRemoved, IReadOnlyList<string> NotDeleted);

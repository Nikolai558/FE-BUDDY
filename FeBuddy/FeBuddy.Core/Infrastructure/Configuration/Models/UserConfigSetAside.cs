namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>What one check of the settings files' layouts set aside (<see cref="UserConfigFile.CheckLayouts"/>), for the notice the GUI shows.</summary>
/// <param name="Folder">Where the files went: <c>%APPDATA%\FE-Buddy\Reverted\&lt;time&gt;</c>.</param>
/// <param name="Files">Each file set aside: the profiles, then <c>Shared.json</c>.</param>
public sealed record UserConfigSetAside(string Folder, IReadOnlyList<UserConfigSetAsideFile> Files);

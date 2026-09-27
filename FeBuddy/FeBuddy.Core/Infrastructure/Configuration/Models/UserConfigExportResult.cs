namespace FeBuddy.Core.Infrastructure.Configuration.Models;

/// <summary>What <see cref="UserConfigTransfer.Export(string)"/> wrote.</summary>
/// <param name="Path">The file written.</param>
/// <param name="SettingCount">How many settings are in it, folders included.</param>
/// <param name="FolderCount">How many of those are folders, written with portable tokens where they could be.</param>
/// <param name="LeftOutCount">How many settings stayed behind because they only belong to this PC.</param>
public sealed record UserConfigExportResult(string Path, int SettingCount, int FolderCount, int LeftOutCount);

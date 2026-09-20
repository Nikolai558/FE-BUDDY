using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>One output file a sub-service run wrote, shown in its result panel.</summary>
/// <param name="Name">The file name on its own, for display.</param>
/// <param name="FullPath">The full path, for opening the folder.</param>
/// <param name="FeatureCount">How many rendered features it holds.</param>
public sealed record SubServiceOutputFileRow(string Name, string FullPath, int FeatureCount);

/// <summary>
/// A sub-service run's messages of one severity, so the result panel can show warnings and
/// errors expanded while routine information stays behind a count.
/// </summary>
/// <param name="Level">The severity these messages share.</param>
/// <param name="Messages">The message texts, in the order the service emitted them.</param>
public sealed record SubServiceMessageGroup(LogLevel Level, IReadOnlyList<string> Messages)
{
    /// <summary>How many messages are in this group.</summary>
    public int Count => Messages.Count;

    /// <summary>The group's heading, e.g. <c>Warning (3)</c>.</summary>
    public string Heading => $"{Level} ({Count})";

    /// <summary>Whether this group is one the user must look at.</summary>
    public bool IsAttentionLevel => Level >= LogLevel.Warning;
}

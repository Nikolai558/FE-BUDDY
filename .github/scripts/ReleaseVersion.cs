// Release-version helper for the release scripts (a .NET 10 file-based app - no project file).
// It runs FeBuddy.Versioning's own rules, so the scripts never hand-parse or hand-compare SemVer.
//
//   dotnet run .github/scripts/ReleaseVersion.cs -- <version> [published release tags...]
//
// Prints key=value lines:
//   releaseShaped  true when <version> is X.Y.Z or X.Y.Z-alpha|beta|rc.N (ReleaseStep.IsReleaseShaped)
//   prerelease     true when <version> has a pre-release tag
//   channel        the update channel it belongs to, e.g. "Alpha" or "Release Candidate"
//   previous       the highest of the published tags (blank when none parse)
//   nextStep       true when <version> is exactly one step after previous (ReleaseStep.IsNextStep)
//   allowed        the versions that would be one step after previous, comma-separated
//
// Exits 2 when <version> is not SemVer at all.

#:project ../../FeBuddy/FeBuddy.Versioning/FeBuddy.Versioning.csproj

using FeBuddy.Versioning;

if (args.Length == 0)
{
	Console.Error.WriteLine("Usage: dotnet run ReleaseVersion.cs -- <version> [published release tags...]");
	return 2;
}

if (!ProductVersion.TryParse(args[0], out ProductVersion? version))
{
	Console.Error.WriteLine($"'{args[0]}' is not a SemVer version.");
	return 2;
}

ProductVersion? previous = ReleaseStep.Latest(args.Skip(1));

Console.WriteLine($"releaseShaped={ReleaseStep.IsReleaseShaped(version!).ToString().ToLowerInvariant()}");
Console.WriteLine($"prerelease={version!.IsPrerelease.ToString().ToLowerInvariant()}");
Console.WriteLine($"channel={version.Channel.DisplayName()}");
Console.WriteLine($"previous={previous}");
Console.WriteLine($"nextStep={ReleaseStep.IsNextStep(previous, version).ToString().ToLowerInvariant()}");
Console.WriteLine($"allowed={(previous is null ? "" : string.Join(", ", ReleaseStep.AllowedNext(previous)))}");
return 0;

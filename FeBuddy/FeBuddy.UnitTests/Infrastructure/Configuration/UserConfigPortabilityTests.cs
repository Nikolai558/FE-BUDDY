using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Exercises <see cref="UserConfigPortability"/>: which settings may leave this PC in an export,
/// and their names in the import summary.
/// </summary>
public sealed class UserConfigPortabilityTests
{
	/// <summary>Facility setup travels; folders, PC-only state, credentials and unknown sections do not travel as-is.</summary>
	[Theory]
	[InlineData("Services.AiracService.UserArtccId", ConfigKeyScope.Shared)]
	[InlineData("Services.AiracService.DefaultRoi.DefaultCoordindates.SwLat", ConfigKeyScope.Shared)]
	[InlineData("Services.MapService.Home", ConfigKeyScope.Shared)]
	[InlineData("General.PrettyPrintGeojson", ConfigKeyScope.Shared)]
	[InlineData(UserConfigKeys.AddFeBuddyOutputFolder, ConfigKeyScope.Shared)]
	[InlineData(UserConfigKeys.DefaultOutputDirectory, ConfigKeyScope.MachinePath)]
	[InlineData("Services.FileConversions.DatToGeojson.SourceFolder", ConfigKeyScope.MachinePath)]
	[InlineData("Services.AiracService.VnasAlias.Sources.1.FilePath", ConfigKeyScope.MachinePath)]
	[InlineData("Services.AiracService.VnasAlias.Sources.1.Url", ConfigKeyScope.Shared)]
	[InlineData("Services.AiracService.VnasAlias.Sources.1.CredentialId", ConfigKeyScope.Shared)]
	[InlineData(UserConfigKeys.UpdateChannel, ConfigKeyScope.Local)]
	[InlineData(UserConfigKeys.NewsLastOpen, ConfigKeyScope.Local)]
	[InlineData("Services.AiracService.AiracCycleId", ConfigKeyScope.Shared)]
	[InlineData(UserConfigKeys.MapOutputGeojson, ConfigKeyScope.Shared)]
	[InlineData("Services.MapService.AiracLayers", ConfigKeyScope.Shared)]
	[InlineData("Window.Left", ConfigKeyScope.Local)]
	[InlineData("Secrets.GitHub.Pat", ConfigKeyScope.Secret)]
	[InlineData("Services.GitHub.PAT", ConfigKeyScope.Secret)]
	[InlineData("Services.Compat", ConfigKeyScope.Shared)]
	[InlineData("General.GitHubToken", ConfigKeyScope.Secret)]
	[InlineData("Services.Vnas.Password", ConfigKeyScope.Secret)]
	[InlineData("Services.Vatusa.apikey", ConfigKeyScope.Secret)]
	[InlineData("Services.Vatsim.Credentials", ConfigKeyScope.Secret)]
	public void classify_sorts_every_kind_of_setting(string key, ConfigKeyScope expected) =>
		Assert.Equal(expected, UserConfigPortability.Classify(key));

	/// <summary>Output folders are written to, so they need not exist yet; others are read from.</summary>
	[Theory]
	[InlineData(UserConfigKeys.DefaultOutputDirectory, true)]
	[InlineData("Services.Other.OutputFolder", true)]
	[InlineData("Services.FileConversions.DatToGeojson.SourceFolder", false)]
	public void is_output_folder_tells_written_folders_from_read_ones(string key, bool expected) =>
		Assert.Equal(expected, UserConfigPortability.IsOutputFolder(key));

	/// <summary>A setting named for a file's path holds a file, not a folder.</summary>
	[Theory]
	[InlineData("Services.AiracService.VnasAlias.Sources.1.FilePath", true)]
	[InlineData("Services.Other.AliasFilePath", true)]
	[InlineData("Services.FileConversions.DatToGeojson.SourceFolder", false)]
	[InlineData("Services.Other.FilePathCount", false)]
	public void is_file_tells_files_from_folders(string key, bool expected) =>
		Assert.Equal(expected, UserConfigPortability.IsFile(key));

	[Fact]
	public void is_file_refuses_null() => Assert.Throws<ArgumentNullException>(() => UserConfigPortability.IsFile(null!));

	/// <summary>Known settings get a name people recognise; anything else shows its key.</summary>
	[Theory]
	[InlineData(UserConfigKeys.DefaultOutputDirectory, "Default output directory")]
	[InlineData(UserConfigKeys.UpdateChannel, "Update channel")]
	[InlineData(UserConfigKeys.NewsLastOpen, "News read status")]
	[InlineData("Services.FileConversions.SctToGeojson.SourceFolder", "SCT2 to GeoJSON source folder")]
	[InlineData("Services.Other.NewThing.SourceFolder", "NewThing source folder")]
	[InlineData("SourceFolder", "SourceFolder")]
	[InlineData("Services.AiracService.VnasAlias.Sources.2.FilePath", "Custom alias file 2")]
	[InlineData("Sources.2.FilePath", "Sources.2.FilePath")]
	[InlineData("Services.Other.2.FilePath", "Services.Other.2.FilePath")]
	[InlineData("Services.AiracService.UserArtccId", "Services.AiracService.UserArtccId")]
	public void describe_names_settings_for_people(string key, string expected) =>
		Assert.Equal(expected, UserConfigPortability.Describe(key));
}

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// What a sub-service writes, for the Outputs card (<c>Views/Cards/OutputsCard</c>), which shows
/// each output as on or off. Both are turned on and off on the General tab.
/// </summary>
public interface IOutputSettings
{
	/// <summary>The sub-service's name.</summary>
	string Title { get; }

	/// <summary>Whether the sub-service's GeoJSON files are written.</summary>
	bool GenerateGeojson { get; }

	/// <summary>Whether the sub-service's alias file is written.</summary>
	bool GenerateAliasFile { get; }
}

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// What a sub-service produces, for the Outputs card (<c>Views/Cards/OutputsCard</c>). Its
/// default GeoJSON row also binds <c>GenerateGeojson</c>; a tab whose GeoJSON choice is more
/// than on / off (Airways) gives the card its own GeoJSON row instead.
/// </summary>
public interface IOutputSettings
{
	/// <summary>The sub-service's name, used in the "at least one output" reminder.</summary>
	string Title { get; }

	/// <summary>Write the sub-service's alias file.</summary>
	bool GenerateAliasFile { get; set; }
}

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The three GeoJSON files a sub-service can write, for the What Files Do You Want? card
/// (<c>Views/Cards/GeojsonFilesCard</c>).
/// </summary>
public interface IGeojsonFileChoices
{
	/// <summary>Write the <c>_Lines</c> file.</summary>
	bool EmitLines { get; set; }

	/// <summary>Write the <c>_Symbols</c> file.</summary>
	bool EmitSymbols { get; set; }

	/// <summary>Write the <c>_Text</c> file.</summary>
	bool EmitText { get; set; }

	/// <summary>The tab's validation messages; the card is outlined while <see cref="ServiceAreas.GeojsonFiles"/> has one.</summary>
	ServiceFieldErrors FieldErrors { get; }
}

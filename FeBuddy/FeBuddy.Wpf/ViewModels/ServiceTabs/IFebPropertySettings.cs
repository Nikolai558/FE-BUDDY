using System.Collections.ObjectModel;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>The FE-Buddy Properties card (<c>Views/Cards/FebPropertiesCard</c>).</summary>
public interface IFebPropertySettings
{
	/// <summary>Whether Features carry the selected <c>feb.*</c> properties.</summary>
	bool IncludeFebCustomProperties { get; set; }

	/// <summary>One toggle per <c>feb.*</c> property this sub-service can write.</summary>
	ObservableCollection<FebPropertyToggle> FebProperties { get; }

	/// <summary>The tab's or page's validation messages; the card is outlined while <see cref="ServiceAreas.FebProperties"/> has one.</summary>
	ServiceFieldErrors FieldErrors { get; }
}

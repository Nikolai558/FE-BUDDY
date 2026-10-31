using System.ComponentModel;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// What the General tab's Alias File Exports card says, as it stands before Save: whether a run also
/// exports each sub-service's own alias file, and the name <c>Combined_Alias.txt</c> is written under. A
/// sub-service tab reads them from here, and follows <see cref="INotifyPropertyChanged.PropertyChanged"/>
/// to keep its note and What You'll Get in step.
/// </summary>
public interface IAliasFileExports : INotifyPropertyChanged
{
	/// <summary>Whether a run also exports each sub-service's own alias file beside the combined one.</summary>
	bool ExportIndividualAliasFiles { get; }

	/// <summary>The name the combined alias file is written under, e.g. <c>Combined_Alias.txt</c>, or a new name from the File Names tab.</summary>
	string CombinedAliasFileName { get; }
}

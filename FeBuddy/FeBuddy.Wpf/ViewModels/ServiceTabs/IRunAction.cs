using System.Windows.Input;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// The Run card (<c>Views/Cards/RunCard</c>): the one button that starts a run, at the foot of
/// the tab that owns it - the Preview Settings tab of AIRAC Services, or a conversion tab.
/// </summary>
public interface IRunAction
{
	/// <summary>The button's label, e.g. <c>Run AIRAC Service</c>.</summary>
	string RunLabel { get; }

	/// <summary>Starts the run.</summary>
	ICommand RunCommand { get; }
}

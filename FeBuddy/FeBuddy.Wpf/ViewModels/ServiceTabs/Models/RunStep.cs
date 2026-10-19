using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// One sub-service's line in the run feed: what it is, where it has got to, and the last thing
/// it reported.
/// </summary>
/// <param name="name">The sub-service's name, as the tab rail shows it.</param>
public sealed class RunStep(string name) : ObservableObject
{
	private RunStepStatus _status = RunStepStatus.Waiting;
	private string? _detail;

	/// <summary>The sub-service's name.</summary>
	public string Name { get; } = name;

	/// <summary>Where it has got to.</summary>
	public RunStepStatus Status
	{
		get => _status;
		set
		{
			if (SetProperty(ref _status, value))
			{
				OnPropertyChanged(nameof(StatusText));
			}
		}
	}

	/// <summary>The status as a word, for the row.</summary>
	public string StatusText => Status switch
	{
		RunStepStatus.Waiting => "waiting",
		RunStepStatus.Working => "working…",
		RunStepStatus.Finished => "finished",
		_ => "failed",
	};

	/// <summary>The last progress message this sub-service reported, if any.</summary>
	public string? Detail
	{
		get => _detail;
		set => SetProperty(ref _detail, value);
	}
}

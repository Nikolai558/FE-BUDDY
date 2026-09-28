namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>Where one sub-service has got to during a run.</summary>
public enum RunStepStatus
{
	/// <summary>Selected for this run, not started yet.</summary>
	Waiting = 0,

	/// <summary>Running now.</summary>
	Working = 1,

	/// <summary>Finished successfully.</summary>
	Finished = 2,

	/// <summary>Did not finish.</summary>
	Failed = 3,
}

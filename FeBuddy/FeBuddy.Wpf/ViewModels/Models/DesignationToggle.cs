using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac.Airways.Models;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One airway designation with an include/exclude toggle, and - for High and Low files - the file
/// its airways go in. The designation is derived from <c>AWY_ID</c> (leading letters); toggles
/// default on, and the <b>deselected</b> set is what gets persisted to <c>ExcludedDesignations</c>.
/// </summary>
/// <param name="designation">The designation, e.g. <c>J</c>.</param>
/// <param name="included">Whether it starts included.</param>
/// <param name="stratum">The High/Low file it starts in, or <see langword="null"/> when none is chosen yet.</param>
/// <param name="onChanged">Called with this toggle when the user includes or excludes it, or changes its file.</param>
public sealed class DesignationToggle(string designation, bool included, AirwayStratum? stratum, Action<DesignationToggle> onChanged) : ObservableObject
{
	private bool _included = included;
	private AirwayStratum? _stratum = stratum;
	private string? _stratumError;

	/// <summary>The designation, e.g. <c>J</c>, <c>V</c>, <c>AT</c>.</summary>
	public string Designation { get; } = designation;

	/// <summary><see langword="true"/> to include this designation in the output.</summary>
	public bool Included
	{
		get => _included;
		set
		{
			if (SetProperty(ref _included, value))
			{
				onChanged(this);
			}
		}
	}

	/// <summary>
	/// Which High/Low file its airways go in - High, Low or Both - or <see langword="null"/> until
	/// the user chooses. Only used when the tab writes High and Low files.
	/// </summary>
	public AirwayStratum? Stratum
	{
		get => _stratum;
		set
		{
			if (SetProperty(ref _stratum, value))
			{
				onChanged(this);
			}
		}
	}

	/// <summary>Why <see cref="Stratum"/> needs a choice, or <see langword="null"/>. Set by the tab.</summary>
	public string? StratumError
	{
		get => _stratumError;
		set => SetProperty(ref _stratumError, value);
	}
}

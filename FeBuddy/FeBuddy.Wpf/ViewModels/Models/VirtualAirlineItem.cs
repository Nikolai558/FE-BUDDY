using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Domain.Telephony;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One virtual airline listed on the Telephony tab's Virtual Airlines card, merged into
/// <c>Telephony.txt</c> with the FAA's operators.
/// </summary>
/// <param name="designator">Its three-letter designator, e.g. <c>DVA</c>.</param>
/// <param name="telephony">Its telephony, e.g. <c>DELTA</c>.</param>
/// <param name="organization">Its virtual organization, e.g. <c>Delta Virtual</c>.</param>
public sealed class VirtualAirlineItem(string designator, string telephony, string organization) : ObservableObject
{
	private string _designator = designator;
	private string _telephony = telephony;
	private string _organization = organization;
	private bool _isOnVatsimRadarList;
	private string? _realOperator;

	/// <summary>Its three-letter designator, e.g. <c>DVA</c>.</summary>
	public string Designator
	{
		get => _designator;
		set
		{
			if (SetProperty(ref _designator, value))
			{
				OnPropertyChanged(nameof(Label));
				OnPropertyChanged(nameof(Commands));
			}
		}
	}

	/// <summary>Its telephony, e.g. <c>DELTA</c>.</summary>
	public string Telephony
	{
		get => _telephony;
		set
		{
			if (SetProperty(ref _telephony, value))
			{
				OnPropertyChanged(nameof(Label));
				OnPropertyChanged(nameof(Commands));
			}
		}
	}

	/// <summary>Its virtual organization, e.g. <c>Delta Virtual</c>.</summary>
	public string Organization { get => _organization; set => SetProperty(ref _organization, value); }

	/// <summary>
	/// Whether the virtual airline list - included on the tab - has one with this 3LD and telephony, so
	/// the list's is left out and only this is written. Set by the tab.
	/// </summary>
	public bool IsOnVatsimRadarList
	{
		get => _isOnVatsimRadarList;
		set
		{
			if (SetProperty(ref _isOnVatsimRadarList, value))
			{
				RaiseNote();
			}
		}
	}

	/// <summary>
	/// The company of the real operator with this 3LD and telephony - so this isn't written - or
	/// <see langword="null"/> when there is none. Set by the tab.
	/// </summary>
	public string? RealOperator
	{
		get => _realOperator;
		set
		{
			if (SetProperty(ref _realOperator, value))
			{
				RaiseNote();
			}
		}
	}

	/// <summary>What the row says about another operator with its 3LD and telephony, in amber; <see langword="null"/> when there is none.</summary>
	public string? Note =>
		RealOperator is { } company ? $"Same 3LD and telephony as a real operator ({company}), so it's left out of Telephony.txt."
		: IsOnVatsimRadarList ? "Same 3LD and telephony as one on the list, so the list's is left out."
		: null;

	/// <summary>Whether <see cref="Note"/> has anything to say.</summary>
	public bool HasNote => Note is not null;

	/// <summary>The list's first line, e.g. <c>DVA · DELTA</c>.</summary>
	public string Label => $"{Designator} · {Telephony}";

	/// <summary>
	/// The commands it shows under, e.g. <c>.idDVA and .idDELTA</c> - one when its telephony spells
	/// its designator, none for a value with no letter or digit.
	/// </summary>
	public string Commands =>
		string.Join(" and ", new[] { TelephonyNaming.CommandName(Designator), TelephonyNaming.CommandName(Telephony) }
			.OfType<string>()
			.Distinct(StringComparer.OrdinalIgnoreCase));

	private void RaiseNote()
	{
		OnPropertyChanged(nameof(Note));
		OnPropertyChanged(nameof(HasNote));
	}
}

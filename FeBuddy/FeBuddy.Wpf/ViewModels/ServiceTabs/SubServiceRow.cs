using System.Runtime.CompilerServices;
using System.Windows;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.Shell;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// One row of the General tab's table: whether a sub-service is in the run, and which of the
/// outputs it offers are on. Its tab reads them from here (<see cref="ISubServiceOutputs"/>).
/// </summary>
/// <remarks>
/// <para>
/// An output the sub-service doesn't offer is always off, and its box is greyed out with a
/// tooltip saying so. An included sub-service keeps at least one output on: unticking the last is
/// refused, with a reminder that unticking Include is how to make nothing for it. The outputs of a
/// sub-service that isn't included are kept as they were and greyed out.
/// </para>
/// <para>
/// The tooltips come from the sub-service's <see cref="SubServiceHelp"/>, each ending with where
/// its settings are: on its tab, to the left of the table.
/// </para>
/// </remarks>
/// <param name="descriptor">The sub-service.</param>
/// <param name="onChanged">Called after the user changes anything on the row.</param>
public sealed class SubServiceRow(SubServiceDescriptor descriptor, Action onChanged) : ObservableObject, ISubServiceOutputs
{
	private static readonly string[] EditStateNames =
		[nameof(CanEditAlias), nameof(CanEditGeojson), nameof(CanEditProcedureChanges), nameof(CanEditProceduresJson)];

	private readonly Action _onChanged = onChanged;
	private bool _isIncluded = true;
	private bool _alias = true;
	private bool _geojson = true;
	private bool _procedureChanges = true;
	private bool _proceduresJson = true;

	/// <summary>The sub-service.</summary>
	public SubServiceDescriptor Descriptor { get; } = descriptor;

	/// <summary>The sub-service's stable key.</summary>
	public string Key => Descriptor.Key;

	/// <summary>The name shown in the table.</summary>
	public string DisplayName => Descriptor.DisplayName;

	/// <summary>Whether the sub-service is in the run. Its tab is greyed out in the rail while it is not.</summary>
	public bool IsIncluded
	{
		get => _isIncluded;
		set
		{
			if (SetProperty(ref _isIncluded, value))
			{
				RaiseEditStates();
				_onChanged();
			}
		}
	}

	/// <inheritdoc />
	public bool Alias
	{
		get => _alias && OffersAlias;
		set => SetOutput(ref _alias, value);
	}

	/// <inheritdoc />
	public bool Geojson
	{
		get => _geojson && OffersGeojson;
		set => SetOutput(ref _geojson, value);
	}

	/// <inheritdoc />
	public bool ProcedureChanges
	{
		get => _procedureChanges && OffersProcedureChanges;
		set => SetOutput(ref _procedureChanges, value);
	}

	/// <inheritdoc />
	public bool ProceduresJson
	{
		get => _proceduresJson && OffersProceduresJson;
		set => SetOutput(ref _proceduresJson, value);
	}

	/// <summary>The outputs that are on, as flags.</summary>
	public SubServiceOutputKinds OutputsOn =>
		(Alias ? SubServiceOutputKinds.Alias : 0) | (Geojson ? SubServiceOutputKinds.Geojson : 0) |
		(ProcedureChanges ? SubServiceOutputKinds.ProcedureChanges : 0) | (ProceduresJson ? SubServiceOutputKinds.ProceduresJson : 0);

	/// <summary>Whether the sub-service has an alias file.</summary>
	public bool OffersAlias => Descriptor.Outputs.HasFlag(SubServiceOutputKinds.Alias);

	/// <summary>Whether the sub-service has GeoJSON files.</summary>
	public bool OffersGeojson => Descriptor.Outputs.HasFlag(SubServiceOutputKinds.Geojson);

	/// <summary>Whether the sub-service writes <c>Procedure_Changes.md</c> (Procedures).</summary>
	public bool OffersProcedureChanges => Descriptor.Outputs.HasFlag(SubServiceOutputKinds.ProcedureChanges);

	/// <summary>Whether the sub-service writes <c>Procedures.json</c> (Procedures).</summary>
	public bool OffersProceduresJson => Descriptor.Outputs.HasFlag(SubServiceOutputKinds.ProceduresJson);

	/// <summary>Whether the Alias box can be changed: offered, and the sub-service included.</summary>
	public bool CanEditAlias => OffersAlias && IsIncluded;

	/// <summary>Whether the GeoJSON box can be changed.</summary>
	public bool CanEditGeojson => OffersGeojson && IsIncluded;

	/// <summary>Whether the Procedure Changes box can be changed.</summary>
	public bool CanEditProcedureChanges => OffersProcedureChanges && IsIncluded;

	/// <summary>Whether the Procedures JSON box can be changed.</summary>
	public bool CanEditProceduresJson => OffersProceduresJson && IsIncluded;

	/// <summary>What the sub-service is, and where its settings are: the Include box's and the name's tooltip.</summary>
	public string IncludeToolTip => $"{Descriptor.Help?.Summary}\n{WhereSettingsAre}";

	/// <summary>The alias file in detail, or that there is none.</summary>
	public string AliasToolTip => OutputToolTip(OffersAlias, Descriptor.Help?.Alias, "an alias file");

	/// <summary>The GeoJSON files in detail, or that there are none.</summary>
	public string GeojsonToolTip => OutputToolTip(OffersGeojson, Descriptor.Help?.Geojson, "GeoJSON files");

	/// <summary><c>Procedure_Changes.md</c> in detail, or that only Procedures writes it.</summary>
	public string ProcedureChangesToolTip => OutputToolTip(OffersProcedureChanges, Descriptor.Help?.ProcedureChanges, "Procedure_Changes.md; only Procedures does");

	/// <summary><c>Procedures.json</c> in detail, or that only Procedures writes it.</summary>
	public string ProceduresJsonToolTip => OutputToolTip(OffersProceduresJson, Descriptor.Help?.ProceduresJson, "Procedures.json; only Procedures does");

	private string WhereSettingsAre => $"Its settings are on the {DisplayName} tab, in the list to the left.";

	private int CountOn => (Alias ? 1 : 0) + (Geojson ? 1 : 0) + (ProcedureChanges ? 1 : 0) + (ProceduresJson ? 1 : 0);

	/// <summary>Restores the row from the saved settings, without telling anyone it changed.</summary>
	/// <param name="included">Whether the sub-service is in the run.</param>
	/// <param name="outputsOn">The outputs that are on; any it doesn't offer are ignored.</param>
	public void Load(bool included, SubServiceOutputKinds outputsOn)
	{
		_isIncluded = included;
		_alias = outputsOn.HasFlag(SubServiceOutputKinds.Alias);
		_geojson = outputsOn.HasFlag(SubServiceOutputKinds.Geojson);
		_procedureChanges = outputsOn.HasFlag(SubServiceOutputKinds.ProcedureChanges);
		_proceduresJson = outputsOn.HasFlag(SubServiceOutputKinds.ProceduresJson);

		// Settings saved with every output off (by hand) would leave it making nothing: all back on.
		if (CountOn == 0)
		{
			_alias = _geojson = _procedureChanges = _proceduresJson = true;
		}

		foreach (string name in new[] { nameof(IsIncluded), nameof(Alias), nameof(Geojson), nameof(ProcedureChanges), nameof(ProceduresJson) })
		{
			OnPropertyChanged(name);
		}

		RaiseEditStates();
	}

	private string OutputToolTip(bool offered, string? help, string what) => offered
		? $"{help}\n{WhereSettingsAre}"
		: $"{DisplayName} doesn't make {what}.";

	private void SetOutput(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
	{
		if (!value && field && CountOn == 1)
		{
			Toast.Warn(
				"Keep one output",
				$"{DisplayName} needs at least one output on. To make nothing for it, untick it under Include.");

			// The box has already changed; put it back once WPF has finished pushing the value in.
			if (Application.Current?.Dispatcher is { } dispatcher)
			{
				dispatcher.BeginInvoke(() => OnPropertyChanged(propertyName));
			}
			else
			{
				OnPropertyChanged(propertyName);
			}

			return;
		}

		if (SetProperty(ref field, value, propertyName))
		{
			_onChanged();
		}
	}

	private void RaiseEditStates()
	{
		foreach (string name in EditStateNames)
		{
			OnPropertyChanged(name);
		}
	}
}

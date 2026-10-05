using System.Runtime.CompilerServices;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.ViewModels.ServiceTabs;

/// <summary>
/// One row of the General tab's table: whether a sub-service is in the run, and which of the
/// outputs it offers are on. Its tab reads them from here (<see cref="ISubServiceOutputs"/>).
/// </summary>
/// <remarks>
/// <para>
/// An output the sub-service doesn't offer is always off, and its box is greyed out with a
/// tooltip saying so. Include always matches "at least one output on" (issue #308): unticking
/// Include unticks every output, and ticking it ticks every output the sub-service offers;
/// unticking the last output unticks Include, and ticking an output of a sub-service left out
/// ticks Include with just that output on.
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

	/// <summary>
	/// Whether the sub-service is in the run. Ticking it ticks every output it offers, and unticking
	/// it unticks them all. Its tab is greyed out in the rail while it is not.
	/// </summary>
	public bool IsIncluded
	{
		get => _isIncluded;
		set
		{
			if (_isIncluded == value)
			{
				return;
			}

			_isIncluded = value;
			_alias = _geojson = _procedureChanges = _proceduresJson = value;
			RaiseRow();
			_onChanged();
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
		_alias = included && outputsOn.HasFlag(SubServiceOutputKinds.Alias);
		_geojson = included && outputsOn.HasFlag(SubServiceOutputKinds.Geojson);
		_procedureChanges = included && outputsOn.HasFlag(SubServiceOutputKinds.ProcedureChanges);
		_proceduresJson = included && outputsOn.HasFlag(SubServiceOutputKinds.ProceduresJson);

		// Included with every output off (settings edited by hand) would make nothing: all on, as
		// ticking Include does.
		if (included && CountOn == 0)
		{
			_alias = _geojson = _procedureChanges = _proceduresJson = true;
		}

		RaiseRow();
	}

	private string OutputToolTip(bool offered, string? help, string what) => offered
		? $"{help}\n{WhereSettingsAre}"
		: $"{DisplayName} doesn't make {what}.";

	/// <summary>Sets an output, then keeps Include matching "at least one output on".</summary>
	private void SetOutput(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
	{
		if (!SetProperty(ref field, value, propertyName))
		{
			return;
		}

		bool included = CountOn > 0;

		if (_isIncluded != included)
		{
			_isIncluded = included;
			OnPropertyChanged(nameof(IsIncluded));
		}

		_onChanged();
	}

	private void RaiseRow()
	{
		foreach (string name in new[] { nameof(IsIncluded), nameof(Alias), nameof(Geojson), nameof(ProcedureChanges), nameof(ProceduresJson) })
		{
			OnPropertyChanged(name);
		}
	}
}

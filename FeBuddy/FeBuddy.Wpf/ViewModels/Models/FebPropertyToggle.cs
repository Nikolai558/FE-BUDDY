using System.Collections.ObjectModel;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.Airac;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One selectable FE-Buddy custom property on a sub-service tab's FE-Buddy Properties card.
/// </summary>
/// <remarks>
/// Every sub-service uses this one type; what differs is the list it is built from
/// (<see cref="AirportFebPropertyOptions"/>, <see cref="AirwayFebPropertyOptions"/>,
/// <see cref="DepartureFebPropertyOptions"/>, <see cref="ArrivalFebPropertyOptions"/>) through
/// <see cref="ListFor"/>.
/// </remarks>
/// <param name="name">Its name as written to the settings block and the GeoJSON key.</param>
/// <param name="description">A short plain-English description for the tooltip.</param>
/// <param name="onChanged">Called when the user ticks or unticks the row.</param>
public sealed class FebPropertyToggle(string name, string description, Action onChanged) : ObservableObject
{
	private readonly Action _onChanged = onChanged;
	private bool _isSelected;

	/// <summary>Builds one toggle per property, named the way Core names it.</summary>
	/// <typeparam name="TProperty">The sub-service's property enum.</typeparam>
	/// <param name="properties">The properties to offer, in display order, with their tooltip text.</param>
	/// <param name="onChanged">Called when the user ticks or unticks any of them.</param>
	/// <returns>The toggles, unticked.</returns>
	public static ObservableCollection<FebPropertyToggle> ListFor<TProperty>(
		IEnumerable<(TProperty Property, string Description)> properties,
		Action onChanged)
		where TProperty : struct, Enum =>
		new(properties.Select(p => new FebPropertyToggle(FebProperties.Name(p.Property), p.Description, onChanged)));

	/// <summary>The settings / JSON name, e.g. <c>faaId</c> - written as <c>feb.faaId</c>.</summary>
	public string Name { get; } = name;

	/// <summary>What the property holds, for the tooltip.</summary>
	public string Description { get; } = description;

	/// <summary>The key as it appears in the file, e.g. <c>feb.faaId</c>.</summary>
	public string JsonKey => $"feb.{Name}";

	/// <summary>Whether the user wants this property written.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				_onChanged();
			}
		}
	}
}

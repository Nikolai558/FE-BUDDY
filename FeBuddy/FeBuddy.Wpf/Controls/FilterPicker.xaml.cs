using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Domain.Crc;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// One number in a <see cref="FilterPicker"/>'s popup.
/// </summary>
/// <param name="number">The value this option stands for.</param>
/// <param name="onChanged">Called when the user ticks or unticks it.</param>
public sealed class FilterOption(int number, Action onChanged) : ObservableObject
{
	private readonly Action _onChanged = onChanged;
	private bool _isSelected;

	/// <summary>The number this option stands for.</summary>
	public int Number { get; } = number;

	/// <summary>Whether it is part of the selection.</summary>
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

	/// <summary>Sets the state without reporting the change, used while syncing from the value.</summary>
	/// <param name="selected">The new state.</param>
	internal void SetSilently(bool selected) => SetProperty(ref _isSelected, selected, nameof(IsSelected));
}

/// <summary>
/// A field that edits a set of numbers from a fixed range - CRC's <c>filters</c> - as a popup of
/// toggles rather than as free text.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Value"/> is the comma-separated string the settings block and <c>UserConfig</c>
/// already use, so nothing downstream changes: the control only replaces how it is typed.
/// Values come back sorted and de-duplicated, which also makes a saved config diff cleanly.
/// </para>
/// <para>
/// The range defaults to what <see cref="CrcPropertyValidator"/> accepts, so the picker
/// cannot offer a number the validator would reject.
/// </para>
/// </remarks>
public partial class FilterPicker : UserControl, INotifyPropertyChanged
{
	private bool _syncing;

	/// <summary>The selected values, comma-separated. Two-way by default.</summary>
	public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
		nameof(Value), typeof(string), typeof(FilterPicker),
		new FrameworkPropertyMetadata(
			string.Empty,
			FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
			OnValueChanged));

	/// <summary>The lowest selectable value.</summary>
	public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
		nameof(Minimum), typeof(int), typeof(FilterPicker),
		new PropertyMetadata(CrcPropertyValidator.MinFilter, OnRangeChanged));

	/// <summary>The highest selectable value.</summary>
	public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
		nameof(Maximum), typeof(int), typeof(FilterPicker),
		new PropertyMetadata(CrcPropertyValidator.MaxFilter, OnRangeChanged));

	/// <summary>Creates the control.</summary>
	public FilterPicker()
	{
		InitializeComponent();
		BuildOptions();
	}

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>The selected values, comma-separated (e.g. <c>3, 7, 12</c>).</summary>
	public string Value
	{
		get => (string)GetValue(ValueProperty);
		set => SetValue(ValueProperty, value);
	}

	/// <summary>The lowest selectable value.</summary>
	public int Minimum
	{
		get => (int)GetValue(MinimumProperty);
		set => SetValue(MinimumProperty, value);
	}

	/// <summary>The highest selectable value.</summary>
	public int Maximum
	{
		get => (int)GetValue(MaximumProperty);
		set => SetValue(MaximumProperty, value);
	}

	/// <summary>The toggles shown in the popup.</summary>
	public ObservableCollection<FilterOption> Options { get; } = [];

	/// <summary>What the closed field shows.</summary>
	public string DisplayText => string.IsNullOrWhiteSpace(Value) ? "none selected" : Value;

	/// <summary>The popup's heading line.</summary>
	public string Prompt => $"Pick every filter this applies to ({Minimum}-{Maximum}). At least one is required.";

	/// <summary>The tool-tip on the closed field.</summary>
	public string SelectionSummary => string.IsNullOrWhiteSpace(Value)
		? "No filters selected yet - click to choose."
		: $"Filters: {Value}";

	private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((FilterPicker)d).SyncOptionsFromValue();

	private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
		((FilterPicker)d).BuildOptions();

	private void BuildOptions()
	{
		Options.Clear();

		for (int number = Minimum; number <= Maximum; number++)
		{
			Options.Add(new FilterOption(number, OnOptionChanged));
		}

		SyncOptionsFromValue();
	}

	/// <summary>Ticks the options named by <see cref="Value"/> and unticks the rest.</summary>
	private void SyncOptionsFromValue()
	{
		if (_syncing)
		{
			return;
		}

		_syncing = true;

		try
		{
			HashSet<int> selected = Parse(Value);

			foreach (FilterOption option in Options)
			{
				option.SetSilently(selected.Contains(option.Number));
			}
		}
		finally
		{
			_syncing = false;
		}

		RaiseDisplayProperties();
	}

	private void OnOptionChanged()
	{
		if (_syncing)
		{
			return;
		}

		_syncing = true;

		try
		{
			Value = string.Join(", ", Options
				.Where(o => o.IsSelected)
				.Select(o => o.Number.ToString(CultureInfo.InvariantCulture)));
		}
		finally
		{
			_syncing = false;
		}

		RaiseDisplayProperties();
	}

	/// <summary>
	/// Reads the comma-separated value. Anything that is not a number in range is dropped: the
	/// picker cannot represent it, and the validator would reject it anyway.
	/// </summary>
	/// <param name="value">The raw value.</param>
	/// <returns>The selected numbers.</returns>
	private HashSet<int> Parse(string? value)
	{
		HashSet<int> selected = [];

		if (string.IsNullOrWhiteSpace(value))
		{
			return selected;
		}

		foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
				&& number >= Minimum
				&& number <= Maximum)
			{
				selected.Add(number);
			}
		}

		return selected;
	}

	/// <summary>
	/// Tells the closed field's bindings to re-read. They bind to this control by name, so they
	/// listen to <see cref="PropertyChanged"/> rather than to a dependency property.
	/// </summary>
	private void RaiseDisplayProperties()
	{
		Raise(nameof(DisplayText));
		Raise(nameof(SelectionSummary));
		Raise(nameof(Prompt));
	}

	private void Raise([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private void OnClear(object sender, RoutedEventArgs e)
	{
		foreach (FilterOption option in Options)
		{
			option.IsSelected = false;
		}
	}

	private void OnDone(object sender, RoutedEventArgs e) => OpenToggle.IsChecked = false;
}

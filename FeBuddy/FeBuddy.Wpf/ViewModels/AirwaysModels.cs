using System.Globalization;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Models.Services.Airac.Airways;
using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>Which CRC ERAM field block a row belongs to.</summary>
public enum EramFieldKind
{
	/// <summary>Line block: bcg, filters, style, thickness.</summary>
	Line,

	/// <summary>Symbol block: bcg, filters, style, size.</summary>
	Symbol,

	/// <summary>Text block: bcg, filters, size, underline, opaque, xOffset, yOffset.</summary>
	Text,
}

/// <summary>
/// One CRC ERAM default row for a single class (e.g. <c>High</c>, <c>Airports</c>) within a
/// kind block (Line / Symbol / Text). Only the fields that apply to the kind are shown:
/// Line has bcg, filters, style and thickness; Symbol has bcg, filters, style and size; Text
/// has bcg, filters, size, underline, opaque, xOffset and yOffset.
/// </summary>
/// <remarks>
/// <para>
/// A row starts empty. FE-Buddy does not pick CRC values on the user's behalf: until they
/// have been chosen (or restored from <c>UserConfig</c>), the owning tab reports them as
/// missing while CRC defaults are switched on, and the empty boxes are marked.
/// </para>
/// <para>
/// <see cref="Underline"/> and <see cref="Opaque"/> hold <c>Y</c> or <c>N</c> - the same
/// spelling that is persisted to <c>UserConfig</c> and sent in the run settings - and are
/// shown as Yes / No through <see cref="YesNoOptions"/>. <see cref="XOffset"/> and
/// <see cref="YOffset"/> hold the text as typed; any whole number (negative included) is
/// valid, and anything else is marked on the box and counts as not filled in.
/// </para>
/// </remarks>
public sealed class EramClassDefault : ObservableObject
{
	private const string RequiredMessage = "Required while CRC ERAM defaults are on.";
	private const string WholeNumberMessage = "Must be a whole number, e.g. 0, 12 or -4.";
	private const string YesNoMessage = "Choose Yes or No.";

	/// <summary>The stored value for "Yes" in <see cref="Underline"/> and <see cref="Opaque"/>.</summary>
	public const string Yes = "Y";

	/// <summary>The stored value for "No" in <see cref="Underline"/> and <see cref="Opaque"/>.</summary>
	public const string No = "N";

	private readonly Action _onChanged;
	private string _bcg = string.Empty;
	private string _filters = string.Empty;
	private string _style = string.Empty;
	private string _thickness = string.Empty;
	private string _size = string.Empty;
	private string _underline = string.Empty;
	private string _opaque = string.Empty;
	private string _xOffset = string.Empty;
	private string _yOffset = string.Empty;
	private bool _isRequired;

	/// <summary>Creates an empty row for <paramref name="className"/> in the <paramref name="kind"/> block.</summary>
	/// <param name="className">The class this row configures, e.g. <c>High</c> or <c>Airports</c>.</param>
	/// <param name="kind">Which block the row belongs to; decides the fields it shows.</param>
	/// <param name="onChanged">Called whenever one of the row's values changes.</param>
	public EramClassDefault(string className, EramFieldKind kind, Action onChanged)
	{
		ClassName = className;
		_onChanged = onChanged;
		ShowStyle = kind is EramFieldKind.Line or EramFieldKind.Symbol;
		ShowThickness = kind is EramFieldKind.Line;
		ShowSize = kind is EramFieldKind.Symbol or EramFieldKind.Text;
		ShowTextOptions = kind is EramFieldKind.Text;

		StyleOptions = kind switch
		{
			EramFieldKind.Line => CrcGeojsonPropertyValidator.ValidLineStyles,
			EramFieldKind.Symbol => CrcGeojsonPropertyValidator.ValidSymbolStyles,
			_ => Array.Empty<string>(),
		};

		SizeOptions = kind is EramFieldKind.Text
			? Range(CrcGeojsonPropertyValidator.MinTextSize, CrcGeojsonPropertyValidator.MaxTextSize)
			: Range(CrcGeojsonPropertyValidator.MinSymbolSize, CrcGeojsonPropertyValidator.MaxSymbolSize);
	}

	/// <summary>The altitude class this row is for (<c>High</c> / <c>Low</c> / <c>Other</c>).</summary>
	public string ClassName { get; }

	/// <summary>Whether the <c>style</c> field applies to this kind.</summary>
	public bool ShowStyle { get; }

	/// <summary>Whether the <c>thickness</c> field applies to this kind.</summary>
	public bool ShowThickness { get; }

	/// <summary>Whether the <c>size</c> field applies to this kind.</summary>
	public bool ShowSize { get; }

	/// <summary>
	/// Whether the Text-only fields (<c>underline</c>, <c>opaque</c>, <c>xOffset</c>,
	/// <c>yOffset</c>) apply to this kind.
	/// </summary>
	public bool ShowTextOptions { get; }

	/// <summary>
	/// The <c>style</c> values CRC accepts for this kind, for the drop-down. Empty for Text,
	/// which has no style.
	/// </summary>
	public IReadOnlyList<string> StyleOptions { get; }

	/// <summary>The <c>bcg</c> values CRC accepts, for the drop-down.</summary>
	public IReadOnlyList<string> BcgOptions { get; } = Range(
		CrcGeojsonPropertyValidator.MinBcg, CrcGeojsonPropertyValidator.MaxBcg);

	/// <summary>The <c>thickness</c> values CRC accepts, for the drop-down. Line only.</summary>
	public IReadOnlyList<string> ThicknessOptions { get; } = Range(
		CrcGeojsonPropertyValidator.MinThickness, CrcGeojsonPropertyValidator.MaxThickness);

	/// <summary>
	/// The <c>size</c> values CRC accepts for this kind, for the drop-down. Symbols and text
	/// have different ranges.
	/// </summary>
	public IReadOnlyList<string> SizeOptions { get; }

	/// <summary>
	/// The choices for the <c>underline</c> and <c>opaque</c> drop-downs. Bind the ComboBox's
	/// <c>SelectedValue</c> with <c>SelectedValuePath="Value"</c>; each option shows as its label.
	/// </summary>
	public IReadOnlyList<YesNoOption> YesNoOptions { get; } = new[]
	{
		new YesNoOption(Yes, "Yes"),
		new YesNoOption(No, "No"),
	};

	private static IReadOnlyList<string> Range(int minimum, int maximum)
	{
		string[] values = new string[maximum - minimum + 1];

		for (int i = 0; i < values.Length; i++)
		{
			values[i] = (minimum + i).ToString(CultureInfo.InvariantCulture);
		}

		return values;
	}

	/// <summary>The CRC <c>bcg</c> value, or empty until chosen.</summary>
	public string Bcg
	{
		get => _bcg;
		set => SetField(ref _bcg, Canonical(value, BcgOptions), nameof(BcgError));
	}

	/// <summary>The CRC <c>filters</c> value (comma-separated), or empty until chosen.</summary>
	public string Filters
	{
		get => _filters;
		set => SetField(ref _filters, value, nameof(FiltersError));
	}

	/// <summary>The CRC <c>style</c> value, or empty until chosen. Line and Symbol only.</summary>
	public string Style
	{
		get => _style;
		set => SetField(ref _style, Canonical(value, StyleOptions), nameof(StyleError));
	}

	/// <summary>The CRC <c>thickness</c> value, or empty until chosen. Line only.</summary>
	public string Thickness
	{
		get => _thickness;
		set => SetField(ref _thickness, Canonical(value, ThicknessOptions), nameof(ThicknessError));
	}

	/// <summary>The CRC <c>size</c> value, or empty until chosen. Symbol and Text only.</summary>
	public string Size
	{
		get => _size;
		set => SetField(ref _size, Canonical(value, SizeOptions), nameof(SizeError));
	}

	/// <summary>The CRC <c>underline</c> value, <c>Y</c> or <c>N</c>, or empty until chosen. Text only.</summary>
	public string Underline
	{
		get => _underline;
		set => SetField(ref _underline, CanonicalYesNo(value), nameof(UnderlineError));
	}

	/// <summary>The CRC <c>opaque</c> value, <c>Y</c> or <c>N</c>, or empty until chosen. Text only.</summary>
	public string Opaque
	{
		get => _opaque;
		set => SetField(ref _opaque, CanonicalYesNo(value), nameof(OpaqueError));
	}

	/// <summary>The CRC <c>xOffset</c> value as typed (any whole number), or empty until entered. Text only.</summary>
	public string XOffset
	{
		get => _xOffset;
		set => SetField(ref _xOffset, value, nameof(XOffsetError));
	}

	/// <summary>The CRC <c>yOffset</c> value as typed (any whole number), or empty until entered. Text only.</summary>
	public string YOffset
	{
		get => _yOffset;
		set => SetField(ref _yOffset, value, nameof(YOffsetError));
	}

	/// <summary>
	/// Whether this row's values are needed right now - CRC defaults are on and the file this
	/// row configures is being written. Set by the owning tab; drives the per-field errors.
	/// </summary>
	public bool IsRequired
	{
		get => _isRequired;
		set
		{
			if (SetProperty(ref _isRequired, value))
			{
				RaiseFieldErrors();
			}
		}
	}

	/// <summary>
	/// Whether any field this row shows is still empty - or, for the Text-only fields, holds a
	/// value that is not usable (an offset that is not a whole number, or an underline / opaque
	/// value other than <c>Y</c> / <c>N</c>).
	/// </summary>
	public bool HasMissingValues =>
		IsBlank(Bcg)
		|| IsBlank(Filters)
		|| (ShowStyle && IsBlank(Style))
		|| (ShowThickness && IsBlank(Thickness))
		|| (ShowSize && IsBlank(Size))
		|| (ShowTextOptions && (!IsYesNo(Underline) || !IsYesNo(Opaque) || !IsInteger(XOffset) || !IsInteger(YOffset)));

	/// <summary>The <c>bcg</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? BcgError => MissingError(true, Bcg);

	/// <summary>The <c>filters</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? FiltersError => MissingError(true, Filters);

	/// <summary>The <c>style</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? StyleError => MissingError(ShowStyle, Style);

	/// <summary>The <c>thickness</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? ThicknessError => MissingError(ShowThickness, Thickness);

	/// <summary>The <c>size</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? SizeError => MissingError(ShowSize, Size);

	/// <summary>The <c>underline</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? UnderlineError => YesNoError(Underline);

	/// <summary>The <c>opaque</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? OpaqueError => YesNoError(Opaque);

	/// <summary>The <c>xOffset</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? XOffsetError => OffsetError(XOffset);

	/// <summary>The <c>yOffset</c> box's error, for <c>infra:FieldState.Error</c>.</summary>
	public string? YOffsetError => OffsetError(YOffset);

	private void SetField(ref string field, string value, string errorPropertyName)
	{
		if (!SetProperty(ref field, value ?? string.Empty))
		{
			return;
		}

		OnPropertyChanged(errorPropertyName);
		_onChanged();
	}

	private string? MissingError(bool applies, string value) =>
		IsRequired && applies && IsBlank(value) ? RequiredMessage : null;

	/// <summary>
	/// Error for an offset box: required-and-blank reads as missing; a non-blank value that is
	/// not a whole number is always reported, so a typo is marked as soon as it is typed.
	/// </summary>
	private string? OffsetError(string value)
	{
		if (!ShowTextOptions)
		{
			return null;
		}

		if (IsBlank(value))
		{
			return IsRequired ? RequiredMessage : null;
		}

		return IsInteger(value) ? null : WholeNumberMessage;
	}

	/// <summary>
	/// Error for an underline / opaque box: required-and-blank reads as missing; a value other
	/// than <c>Y</c> / <c>N</c> (only possible from a hand-edited config) is always reported.
	/// </summary>
	private string? YesNoError(string value)
	{
		if (!ShowTextOptions)
		{
			return null;
		}

		if (IsBlank(value))
		{
			return IsRequired ? RequiredMessage : null;
		}

		return IsYesNo(value) ? null : YesNoMessage;
	}

	private void RaiseFieldErrors()
	{
		OnPropertyChanged(nameof(BcgError));
		OnPropertyChanged(nameof(FiltersError));
		OnPropertyChanged(nameof(StyleError));
		OnPropertyChanged(nameof(ThicknessError));
		OnPropertyChanged(nameof(SizeError));
		OnPropertyChanged(nameof(UnderlineError));
		OnPropertyChanged(nameof(OpaqueError));
		OnPropertyChanged(nameof(XOffsetError));
		OnPropertyChanged(nameof(YOffsetError));
	}

	private static bool IsBlank(string value) => string.IsNullOrWhiteSpace(value);

	private static bool IsInteger(string value) =>
		int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

	private static bool IsYesNo(string value) => value is Yes or No;

	/// <summary>
	/// Returns <c>Y</c> or <c>N</c> for any spelling of yes / no (<c>y</c>, <c>Yes</c>,
	/// <c>true</c>, ...), so a value restored from config still selects in the drop-down. A
	/// value that matches neither is kept as it is, for the row to report.
	/// </summary>
	/// <param name="value">The incoming value.</param>
	/// <returns>The canonical value.</returns>
	private static string CanonicalYesNo(string? value)
	{
		if (value is null)
		{
			return string.Empty;
		}

		string trimmed = value.Trim();

		if (trimmed.Equals("Y", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("Yes", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
		{
			return Yes;
		}

		if (trimmed.Equals("N", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("No", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
		{
			return No;
		}

		return value;
	}

	/// <summary>
	/// Returns the option matching <paramref name="value"/> in its canonical spelling, so a
	/// value restored from config in another case still selects in the drop-down. A value that
	/// matches nothing is kept as it is, for the validator to report.
	/// </summary>
	/// <param name="value">The incoming value.</param>
	/// <param name="options">The allowed values for this field.</param>
	/// <returns>The canonical value.</returns>
	private static string Canonical(string? value, IReadOnlyList<string> options)
	{
		if (value is null)
		{
			return string.Empty;
		}

		foreach (string option in options)
		{
			if (string.Equals(option, value, StringComparison.OrdinalIgnoreCase))
			{
				return option;
			}
		}

		return value;
	}
}

/// <summary>
/// One entry in a Yes / No drop-down: the stored <paramref name="Value"/> (<c>Y</c> / <c>N</c>)
/// and the <paramref name="Label"/> the user sees.
/// </summary>
/// <param name="Value">The stored value, <c>Y</c> or <c>N</c>.</param>
/// <param name="Label">The text shown in the drop-down, <c>Yes</c> or <c>No</c>.</param>
public sealed record YesNoOption(string Value, string Label)
{
	/// <summary>Returns <see cref="Label"/>, which is what the ComboBox displays.</summary>
	/// <returns>The label.</returns>
	public override string ToString() => Label;
}

/// <summary>
/// One airway designation with an include/exclude toggle (remediation plan 7.4). The
/// designation is derived from <c>AWY_ID</c> (leading letters); toggles default on, and the
/// <b>deselected</b> set is what gets persisted to <c>ExcludedDesignations</c>.
/// </summary>
public sealed class DesignationToggle(string designation, bool included, Action onChanged) : ObservableObject
{
	private bool _included = included;

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
				onChanged();
			}
		}
	}
}

/// <summary>
/// One selectable FE-Buddy custom property on the Airways tab.
/// </summary>
/// <remarks>
/// The Airways counterpart of <see cref="DepartureFebPropertyToggle"/>, which is tied to the
/// Departures property enum.
/// </remarks>
public sealed class AirwayFebPropertyToggle : ObservableObject
{
	private readonly Action _onChanged;
	private bool _isSelected;

	/// <summary>Creates a toggle.</summary>
	/// <param name="property">The property this row controls.</param>
	/// <param name="name">Its name as written to the settings block and the GeoJSON key.</param>
	/// <param name="description">A short plain-English description for the tooltip.</param>
	/// <param name="onChanged">Called when the user ticks or unticks the row.</param>
	public AirwayFebPropertyToggle(AirwayFebProperty property, string name, string description, Action onChanged)
	{
		Property = property;
		Name = name;
		Description = description;
		_onChanged = onChanged;
	}

	/// <summary>The property this row controls.</summary>
	public AirwayFebProperty Property { get; }

	/// <summary>The settings / JSON name, e.g. <c>awyId</c> - written as <c>feb.awyId</c>.</summary>
	public string Name { get; }

	/// <summary>What the property holds, for the tooltip.</summary>
	public string Description { get; }

	/// <summary>The key as it appears in the file, e.g. <c>feb.awyId</c>.</summary>
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

/// <summary>
/// The FE-Buddy custom properties an airway Feature can carry, with the names the settings
/// block and the GeoJSON keys use.
/// </summary>
/// <remarks>
/// The names here must match <c>AirwaySettingsParser</c>'s own list exactly - it rejects a
/// name it does not recognize - so this is the GUI-side half of one contract, kept in one place
/// rather than spelled out in XAML.
/// </remarks>
public static class AirwayFebPropertyNames
{
	/// <summary>Every property, in the order the tab lists them.</summary>
	public static IReadOnlyList<(AirwayFebProperty Property, string Name, string Description)> All { get; } = new[]
	{
		(AirwayFebProperty.AwyId, "awyId", "The airway ID. On Symbols and Text, every airway in the file that uses the point."),
		(AirwayFebProperty.PointId, "pointId", "The waypoint's ID. Symbols only."),
		(AirwayFebProperty.Waypoints, "waypoints", "The airway's waypoint IDs, in order. Lines only."),
	};
}

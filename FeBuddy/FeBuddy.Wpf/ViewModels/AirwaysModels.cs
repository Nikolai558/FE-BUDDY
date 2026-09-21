using System.Globalization;

using FeBuddy.Wpf.Infrastructure;

using FeBuddy.Core.Services.General;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One GeoJSON file written by a real Airways run, shown in the results list.
/// </summary>
public sealed record AirwaysOutputFileRow(string Name, string FullPath, int FeatureCount);

/// <summary>
/// Every message from a real Airways run that names the same airway (e.g. "Airway
/// 'T312': ..."), grouped so the results panel doesn't show one flat wall of text.
/// Messages that don't name a specific airway (e.g. an unrecognized setting) are
/// grouped under "General". Each group also carries the highest level it contains so the
/// panel can present it by severity (remediation plan 3.8).
/// </summary>
public sealed record AirwaysMessageGroup(string AirwayId, LogLevel Level, IReadOnlyList<string> Messages)
{
	/// <summary>How many messages are in this group.</summary>
	public int Count => Messages.Count;
}

/// <summary>Which CRC ERAM field block a row belongs to.</summary>
public enum EramFieldKind
{
	/// <summary>Line block: bcg, filters, style, thickness.</summary>
	Line,

	/// <summary>Symbol block: bcg, filters, style, size.</summary>
	Symbol,

	/// <summary>Text block: bcg, filters, size.</summary>
	Text,
}

/// <summary>
/// One CRC ERAM default row for a single altitude class within a kind block
/// (remediation plan 7.4 - three stacked blocks, no LINE/SYMBOL/TEXT selector). Only the
/// fields that apply to the kind are shown.
/// </summary>
/// <remarks>
/// A row starts empty. FE-Buddy does not pick CRC values on the user's behalf: until they
/// have been chosen (or restored from <c>UserConfig</c>), the owning tab reports them as
/// missing while CRC defaults are switched on, and the empty boxes are marked.
/// </remarks>
public sealed class EramClassDefault : ObservableObject
{
	private const string RequiredMessage = "Required while CRC ERAM defaults are on.";

	private readonly Action _onChanged;
	private string _bcg = string.Empty;
	private string _filters = string.Empty;
	private string _style = string.Empty;
	private string _thickness = string.Empty;
	private string _size = string.Empty;
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

	/// <summary>Whether any field this row shows is still empty.</summary>
	public bool HasMissingValues =>
		IsBlank(Bcg)
		|| IsBlank(Filters)
		|| (ShowStyle && IsBlank(Style))
		|| (ShowThickness && IsBlank(Thickness))
		|| (ShowSize && IsBlank(Size));

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

	private void RaiseFieldErrors()
	{
		OnPropertyChanged(nameof(BcgError));
		OnPropertyChanged(nameof(FiltersError));
		OnPropertyChanged(nameof(StyleError));
		OnPropertyChanged(nameof(ThicknessError));
		OnPropertyChanged(nameof(SizeError));
	}

	private static bool IsBlank(string value) => string.IsNullOrWhiteSpace(value);

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

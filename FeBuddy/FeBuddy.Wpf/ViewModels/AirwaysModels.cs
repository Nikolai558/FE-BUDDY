using FeBuddy.Wpf.Infrastructure;

using FEBuddyLibrary.Services.General;

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
public sealed class EramClassDefault : ObservableObject
{
	private readonly Action _onChanged;
	private string _bcg;
	private string _filters;
	private string _style;
	private string _thickness = "1";
	private string _size = "1";

	/// <summary>Creates a row for <paramref name="className"/> in the <paramref name="kind"/> block.</summary>
	public EramClassDefault(string className, EramFieldKind kind, string bcg, string filters, string style, Action onChanged)
	{
		ClassName = className;
		_bcg = bcg;
		_filters = filters;
		_style = style;
		_onChanged = onChanged;
		ShowStyle = kind is EramFieldKind.Line or EramFieldKind.Symbol;
		ShowThickness = kind is EramFieldKind.Line;
		ShowSize = kind is EramFieldKind.Symbol or EramFieldKind.Text;
	}

	/// <summary>The altitude class this row is for (<c>High</c> / <c>Low</c> / <c>Other</c>).</summary>
	public string ClassName { get; }

	/// <summary>Whether the <c>style</c> field applies to this kind.</summary>
	public bool ShowStyle { get; }

	/// <summary>Whether the <c>thickness</c> field applies to this kind.</summary>
	public bool ShowThickness { get; }

	/// <summary>Whether the <c>size</c> field applies to this kind.</summary>
	public bool ShowSize { get; }

	public string Bcg { get => _bcg; set { if (SetProperty(ref _bcg, value)) _onChanged(); } }
	public string Filters { get => _filters; set { if (SetProperty(ref _filters, value)) _onChanged(); } }
	public string Style { get => _style; set { if (SetProperty(ref _style, value)) _onChanged(); } }
	public string Thickness { get => _thickness; set { if (SetProperty(ref _thickness, value)) _onChanged(); } }
	public string Size { get => _size; set { if (SetProperty(ref _size, value)) _onChanged(); } }
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

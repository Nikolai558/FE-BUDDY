using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>One file's CRC ERAM defaults inside the CRC ERAM Defaults card. See CrcDefaultsPanel.xaml.</summary>
public partial class CrcDefaultsPanel : UserControl
{
	/// <summary>Cell width with one class: the panel has room for wide boxes.</summary>
	private const double SingleClassCellWidth = 160;

	/// <summary>Cell width per class when several sit side by side (Airways: High / Low / Other).</summary>
	private const double MultiClassCellWidth = 110;

	/// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
		nameof(Title), typeof(string), typeof(CrcDefaultsPanel), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="Classes"/> dependency property.</summary>
	public static readonly DependencyProperty ClassesProperty = DependencyProperty.Register(
		nameof(Classes), typeof(IReadOnlyList<EramClassDefault>), typeof(CrcDefaultsPanel),
		new PropertyMetadata(null, OnClassesChanged));

	/// <summary>Identifies the <see cref="ShowInclude"/> dependency property.</summary>
	public static readonly DependencyProperty ShowIncludeProperty = DependencyProperty.Register(
		nameof(ShowInclude), typeof(bool), typeof(CrcDefaultsPanel), new PropertyMetadata(false));

	/// <summary>Identifies the <see cref="IsIncluded"/> dependency property.</summary>
	public static readonly DependencyProperty IsIncludedProperty = DependencyProperty.Register(
		nameof(IsIncluded), typeof(bool), typeof(CrcDefaultsPanel),
		new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	private static readonly DependencyPropertyKey CellWidthKey = DependencyProperty.RegisterReadOnly(
		nameof(CellWidth), typeof(double), typeof(CrcDefaultsPanel), new PropertyMetadata(SingleClassCellWidth));

	/// <summary>Identifies the read-only <see cref="CellWidth"/> dependency property.</summary>
	public static readonly DependencyProperty CellWidthProperty = CellWidthKey.DependencyProperty;

	private static readonly DependencyPropertyKey CellMarginKey = DependencyProperty.RegisterReadOnly(
		nameof(CellMargin), typeof(Thickness), typeof(CrcDefaultsPanel), new PropertyMetadata(new Thickness(0, 3, 0, 3)));

	/// <summary>Identifies the read-only <see cref="CellMargin"/> dependency property.</summary>
	public static readonly DependencyProperty CellMarginProperty = CellMarginKey.DependencyProperty;

	private static readonly DependencyPropertyKey ShowClassNamesKey = DependencyProperty.RegisterReadOnly(
		nameof(ShowClassNames), typeof(bool), typeof(CrcDefaultsPanel), new PropertyMetadata(false));

	/// <summary>Identifies the read-only <see cref="ShowClassNames"/> dependency property.</summary>
	public static readonly DependencyProperty ShowClassNamesProperty = ShowClassNamesKey.DependencyProperty;

	private static readonly DependencyPropertyKey BandsKey = DependencyProperty.RegisterReadOnly(
		nameof(Bands), typeof(IReadOnlyList<IReadOnlyList<EramClassDefault>>), typeof(CrcDefaultsPanel),
		new PropertyMetadata(Array.Empty<IReadOnlyList<EramClassDefault>>()));

	/// <summary>Identifies the read-only <see cref="Bands"/> dependency property.</summary>
	public static readonly DependencyProperty BandsProperty = BandsKey.DependencyProperty;

	/// <summary>How many class columns each band holds now; the last band may hold fewer.</summary>
	private int _columnsPerBand;

	/// <summary>Creates the panel.</summary>
	public CrcDefaultsPanel() => InitializeComponent();

	/// <summary>The panel's heading, e.g. "Runways — Lines". Written in normal case.</summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>The defaults to edit, one per class in use; all of the same kind. Empty hides the panel.</summary>
	public IReadOnlyList<EramClassDefault>? Classes
	{
		get => (IReadOnlyList<EramClassDefault>?)GetValue(ClassesProperty);
		set => SetValue(ClassesProperty, value);
	}

	/// <summary>Whether the title has an Include box (the File Conversions). Off by default.</summary>
	public bool ShowInclude
	{
		get => (bool)GetValue(ShowIncludeProperty);
		set => SetValue(ShowIncludeProperty, value);
	}

	/// <summary>
	/// Whether this kind gets the CRC ERAM defaults (the Include box). Two-way; on by default, so a
	/// panel without the box is always editable.
	/// </summary>
	public bool IsIncluded
	{
		get => (bool)GetValue(IsIncludedProperty);
		set => SetValue(IsIncludedProperty, value);
	}

	/// <summary>Width of one class's cell; narrower when several classes share the panel.</summary>
	public double CellWidth => (double)GetValue(CellWidthProperty);

	/// <summary>Space around one class's cell; columns get a gutter when there are several.</summary>
	public Thickness CellMargin => (Thickness)GetValue(CellMarginProperty);

	/// <summary>Whether the class-name row shows: only when there is more than one class to tell apart.</summary>
	public bool ShowClassNames => (bool)GetValue(ShowClassNamesProperty);

	/// <summary>
	/// <see cref="Classes"/> split into the lines of columns the panel draws, as many to a line as
	/// fit the width it is given. One band while they all fit - always, for a single class.
	/// </summary>
	public IReadOnlyList<IReadOnlyList<EramClassDefault>> Bands => (IReadOnlyList<IReadOnlyList<EramClassDefault>>)GetValue(BandsProperty);

	/// <inheritdoc />
	/// <remarks>
	/// Re-forms <see cref="Bands"/> for the width on offer before measuring: first measures the
	/// panel with unlimited width to learn how much of it is not class columns (the border, its
	/// padding and the row names), then fits as many fixed-width columns as the rest allows, at
	/// least one to a band. Runs on every resize, so the bands follow the window.
	/// </remarks>
	protected override Size MeasureOverride(Size constraint)
	{
		int count = Classes?.Count ?? 0;

		if (count > 1 && !double.IsInfinity(constraint.Width))
		{
			double column = CellWidth + CellMargin.Left + CellMargin.Right;
			Size natural = base.MeasureOverride(new Size(double.PositiveInfinity, constraint.Height));
			double overhead = natural.Width - (_columnsPerBand * column);
			int fit = Math.Clamp((int)Math.Floor((constraint.Width - overhead) / column), 1, count);

			if (fit != _columnsPerBand)
			{
				SetBands(fit);
			}
		}

		return base.MeasureOverride(constraint);
	}

	private static void OnClassesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var panel = (CrcDefaultsPanel)d;
		int count = (e.NewValue as IReadOnlyList<EramClassDefault>)?.Count ?? 0;
		bool several = count > 1;

		panel.SetValue(CellWidthKey, several ? MultiClassCellWidth : SingleClassCellWidth);
		panel.SetValue(CellMarginKey, several ? new Thickness(4, 3, 4, 3) : new Thickness(0, 3, 0, 3));
		panel.SetValue(ShowClassNamesKey, several);

		// Every class on one line until the next measure knows the width.
		panel.SetBands(count);

		// No file of this kind gets CRC-ERAM defaults, so there is nothing to fill in.
		panel.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}

	/// <summary>Splits <see cref="Classes"/> into bands of <paramref name="columnsPerBand"/>.</summary>
	/// <param name="columnsPerBand">Columns per band; the last band takes what is left.</param>
	private void SetBands(int columnsPerBand)
	{
		IReadOnlyList<EramClassDefault> classes = Classes ?? [];
		_columnsPerBand = Math.Max(1, Math.Min(columnsPerBand, classes.Count));

		SetValue(BandsKey, classes.Count == 0
			? Array.Empty<IReadOnlyList<EramClassDefault>>()
			: [.. classes.Chunk(_columnsPerBand).Select(band => (IReadOnlyList<EramClassDefault>)band)]);
	}
}

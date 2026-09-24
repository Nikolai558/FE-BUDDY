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

	/// <summary>Identifies the <see cref="IsIncluded"/> dependency property.</summary>
	public static readonly DependencyProperty IsIncludedProperty = DependencyProperty.Register(
		nameof(IsIncluded), typeof(bool), typeof(CrcDefaultsPanel),
		new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	/// <summary>Identifies the <see cref="Classes"/> dependency property.</summary>
	public static readonly DependencyProperty ClassesProperty = DependencyProperty.Register(
		nameof(Classes), typeof(IReadOnlyList<EramClassDefault>), typeof(CrcDefaultsPanel),
		new PropertyMetadata(null, OnClassesChanged));

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

	/// <summary>Creates the panel.</summary>
	public CrcDefaultsPanel() => InitializeComponent();

	/// <summary>The panel's heading, e.g. "Runways — Lines". Written in normal case.</summary>
	public string Title
	{
		get => (string)GetValue(TitleProperty);
		set => SetValue(TitleProperty, value);
	}

	/// <summary>Whether this file gets the CRC ERAM defaults (the Include box). Two-way.</summary>
	public bool IsIncluded
	{
		get => (bool)GetValue(IsIncludedProperty);
		set => SetValue(IsIncludedProperty, value);
	}

	/// <summary>The defaults to edit, one per class; all of the same kind.</summary>
	public IReadOnlyList<EramClassDefault>? Classes
	{
		get => (IReadOnlyList<EramClassDefault>?)GetValue(ClassesProperty);
		set => SetValue(ClassesProperty, value);
	}

	/// <summary>Width of one class's cell; narrower when several classes share the panel.</summary>
	public double CellWidth => (double)GetValue(CellWidthProperty);

	/// <summary>Space around one class's cell; columns get a gutter when there are several.</summary>
	public Thickness CellMargin => (Thickness)GetValue(CellMarginProperty);

	/// <summary>Whether the class-name row shows: only when there is more than one class to tell apart.</summary>
	public bool ShowClassNames => (bool)GetValue(ShowClassNamesProperty);

	private static void OnClassesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var panel = (CrcDefaultsPanel)d;
		bool several = e.NewValue is IReadOnlyList<EramClassDefault> { Count: > 1 };

		panel.SetValue(CellWidthKey, several ? MultiClassCellWidth : SingleClassCellWidth);
		panel.SetValue(CellMarginKey, several ? new Thickness(4, 3, 4, 3) : new Thickness(0, 3, 0, 3));
		panel.SetValue(ShowClassNamesKey, several);
	}
}

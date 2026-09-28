using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.Models;

namespace FeBuddy.Wpf.Views.Cards;

/// <summary>One file's CRC ERAM defaults inside the CRC ERAM Defaults card. See CrcDefaultsPanel.xaml.</summary>
public partial class CrcDefaultsPanel : UserControl
{
	/// <summary>Control width with one class: the panel has room for wide boxes.</summary>
	private const double SingleClassCellWidth = 160;

	/// <summary>Control width when there are several class blocks (Airways High / Low / Other, NAVAIDs by type).</summary>
	private const double MultiClassCellWidth = 130;

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

	private static readonly DependencyPropertyKey BlockMarginKey = DependencyProperty.RegisterReadOnly(
		nameof(BlockMargin), typeof(Thickness), typeof(CrcDefaultsPanel), new PropertyMetadata(new Thickness(0)));

	/// <summary>Identifies the read-only <see cref="BlockMargin"/> dependency property.</summary>
	public static readonly DependencyProperty BlockMarginProperty = BlockMarginKey.DependencyProperty;

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

	/// <summary>Width of one control; narrower when there are several class blocks.</summary>
	public double CellWidth => (double)GetValue(CellWidthProperty);

	/// <summary>Space around one class block: a gap to the next block when there are several.</summary>
	public Thickness BlockMargin => (Thickness)GetValue(BlockMarginProperty);

	/// <summary>Whether each block shows its class name: only when there is more than one class to tell apart.</summary>
	public bool ShowClassNames => (bool)GetValue(ShowClassNamesProperty);

	private static void OnClassesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var panel = (CrcDefaultsPanel)d;
		int count = (e.NewValue as IReadOnlyList<EramClassDefault>)?.Count ?? 0;
		bool several = count > 1;

		panel.SetValue(CellWidthKey, several ? MultiClassCellWidth : SingleClassCellWidth);
		panel.SetValue(BlockMarginKey, several ? new Thickness(0, 0, 24, 10) : new Thickness(0));
		panel.SetValue(ShowClassNamesKey, several);

		// No file of this kind gets CRC-ERAM defaults, so there is nothing to fill in.
		panel.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
	}
}

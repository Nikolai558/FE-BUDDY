using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Lays out a table of commands, three children to a row - syntax, description, examples - with
/// the column headings as the first row. The Alias Command Guide page uses it for each command table.
/// <code>
/// &lt;ctl:CommandTablePanel RuleBrush="{DynamicResource Brush.Stroke}"&gt;
///     &lt;TextBlock Text="SYNTAX" /&gt; &lt;TextBlock Text="DESCRIPTION" /&gt; &lt;TextBlock Text="EXAMPLE" /&gt;
///     &lt;StackPanel /&gt; &lt;TextBlock TextWrapping="Wrap" /&gt; &lt;StackPanel /&gt;
/// &lt;/ctl:CommandTablePanel&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The first and last columns never wrap: each is as wide as its widest cell, measured with all
/// the room it wants, and the middle column has the rest. When the rest would be narrower than
/// <see cref="MiddleMinWidth"/>, each row stacks instead - its three cells one under another,
/// across the whole width - and the headings are hidden.
/// </para>
/// <para>
/// The choice is made from the width the panel is given and the first and last cells' own widths,
/// never from how the middle cells wrap, so the table cannot flip back and forth while it is laid
/// out. A rule in <see cref="RuleBrush"/> runs between rows. Children past the last whole row are
/// not laid out.
/// </para>
/// </remarks>
public sealed class CommandTablePanel : Panel
{
	/// <summary>Identifies the <see cref="MiddleMinWidth"/> dependency property.</summary>
	public static readonly DependencyProperty MiddleMinWidthProperty = DependencyProperty.Register(
		nameof(MiddleMinWidth), typeof(double), typeof(CommandTablePanel),
		new FrameworkPropertyMetadata(240.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="ColumnSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty ColumnSpacingProperty = DependencyProperty.Register(
		nameof(ColumnSpacing), typeof(double), typeof(CommandTablePanel),
		new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="RowPadding"/> dependency property.</summary>
	public static readonly DependencyProperty RowPaddingProperty = DependencyProperty.Register(
		nameof(RowPadding), typeof(double), typeof(CommandTablePanel),
		new FrameworkPropertyMetadata(12.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="StackSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty StackSpacingProperty = DependencyProperty.Register(
		nameof(StackSpacing), typeof(double), typeof(CommandTablePanel),
		new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="RuleBrush"/> dependency property.</summary>
	public static readonly DependencyProperty RuleBrushProperty = DependencyProperty.Register(
		nameof(RuleBrush), typeof(Brush), typeof(CommandTablePanel),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

	/// <summary>The rules' thickness.</summary>
	private const double RuleThickness = 1;

	/// <summary>Where each rule's top edge is, from the last layout.</summary>
	private readonly List<double> _rules = [];

	/// <summary>The first and last columns' widths, from the last measure.</summary>
	private double _firstWidth, _lastWidth;

	/// <summary>The least width the middle column keeps beside the other two; with less, every row stacks.</summary>
	public double MiddleMinWidth
	{
		get => (double)GetValue(MiddleMinWidthProperty);
		set => SetValue(MiddleMinWidthProperty, value);
	}

	/// <summary>The gap between two columns.</summary>
	public double ColumnSpacing
	{
		get => (double)GetValue(ColumnSpacingProperty);
		set => SetValue(ColumnSpacingProperty, value);
	}

	/// <summary>The space between a row and the rule above or below it.</summary>
	public double RowPadding
	{
		get => (double)GetValue(RowPaddingProperty);
		set => SetValue(RowPaddingProperty, value);
	}

	/// <summary>The gap between a stacked row's cells.</summary>
	public double StackSpacing
	{
		get => (double)GetValue(StackSpacingProperty);
		set => SetValue(StackSpacingProperty, value);
	}

	/// <summary>The brush the rules between rows are drawn in; none when <see langword="null"/>.</summary>
	public Brush? RuleBrush
	{
		get => (Brush?)GetValue(RuleBrushProperty);
		set => SetValue(RuleBrushProperty, value);
	}

	/// <summary>Whether the last layout put the cells in columns, rather than stacking each row.</summary>
	internal bool IsColumns { get; private set; }

	/// <summary>The rules' top edges from the last layout, for the tests.</summary>
	internal IReadOnlyList<double> Rules => _rules;

	private int RowCount => InternalChildren.Count / 3;

	private double RowGap => RowPadding + RuleThickness + RowPadding;

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		int rows = RowCount;
		double width = availableSize.Width;
		Size unbounded = new(double.PositiveInfinity, double.PositiveInfinity);

		for (int i = rows * 3; i < InternalChildren.Count; i++)
		{
			InternalChildren[i].Measure(default);
		}

		(_firstWidth, _lastWidth) = (0, 0);

		for (int row = 0; row < rows; row++)
		{
			Cell(row, 0).Measure(unbounded);
			Cell(row, 2).Measure(unbounded);
			_firstWidth = Math.Max(_firstWidth, Cell(row, 0).DesiredSize.Width);
			_lastWidth = Math.Max(_lastWidth, Cell(row, 2).DesiredSize.Width);
		}

		double middleWidth = width - _firstWidth - _lastWidth - (2 * ColumnSpacing);
		IsColumns = !double.IsInfinity(width) && middleWidth >= MiddleMinWidth;

		double height = 0;

		if (IsColumns)
		{
			for (int row = 0; row < rows; row++)
			{
				Cell(row, 1).Measure(new Size(middleWidth, double.PositiveInfinity));
				height += (row > 0 ? RowGap : 0) + ColumnsRowHeight(row);
			}

			return new Size(width, height);
		}

		double widest = 0;

		for (int row = 0; row < rows; row++)
		{
			// The headings name columns, so with no columns they are hidden.
			if (row == 0)
			{
				Cell(row, 1).Measure(default);
				continue;
			}

			for (int column = 0; column < 3; column++)
			{
				Cell(row, column).Measure(new Size(width, double.PositiveInfinity));
				widest = Math.Max(widest, Cell(row, column).DesiredSize.Width);
			}

			height += (row > 1 ? RowGap : 0) + StackedRowHeight(row);
		}

		return new Size(double.IsInfinity(width) ? widest : width, height);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		int rows = RowCount;
		List<double> rules = [];
		double y = 0;

		if (IsColumns)
		{
			double middleWidth = Math.Max(0, finalSize.Width - _firstWidth - _lastWidth - (2 * ColumnSpacing));

			for (int row = 0; row < rows; row++)
			{
				y = NextRow(y, row > 0, rules);
				double height = ColumnsRowHeight(row);

				Cell(row, 0).Arrange(new Rect(0, y, _firstWidth, height));
				Cell(row, 1).Arrange(new Rect(_firstWidth + ColumnSpacing, y, middleWidth, height));
				Cell(row, 2).Arrange(new Rect(finalSize.Width - _lastWidth, y, _lastWidth, height));
				y += height;
			}
		}
		else
		{
			for (int row = 0; row < rows; row++)
			{
				if (row == 0)
				{
					for (int column = 0; column < 3; column++)
					{
						Cell(row, column).Arrange(new Rect());
					}

					continue;
				}

				y = NextRow(y, row > 1, rules);
				bool placed = false;

				// An empty cell takes no room, and no gap either.
				for (int column = 0; column < 3; column++)
				{
					UIElement cell = Cell(row, column);
					double height = cell.DesiredSize.Height;

					if (height > 0 && placed)
					{
						y += StackSpacing;
					}

					cell.Arrange(new Rect(0, y, finalSize.Width, height));
					y += height;
					placed |= height > 0;
				}
			}
		}

		for (int i = rows * 3; i < InternalChildren.Count; i++)
		{
			InternalChildren[i].Arrange(new Rect());
		}

		// A rule can move while the panel keeps its size; only then does it need drawing again.
		if (!rules.SequenceEqual(_rules))
		{
			_rules.Clear();
			_rules.AddRange(rules);
			InvalidateVisual();
		}

		return finalSize;
	}

	/// <inheritdoc />
	protected override void OnRender(DrawingContext drawingContext)
	{
		base.OnRender(drawingContext);

		if (RuleBrush is not { } brush)
		{
			return;
		}

		foreach (double top in _rules)
		{
			drawingContext.DrawRectangle(brush, null, new Rect(0, top, ActualWidth, RuleThickness));
		}
	}

	private UIElement Cell(int row, int column) => InternalChildren[(row * 3) + column];

	/// <summary>Where a row starts: after the rule above it, when it has one (noted in <paramref name="rules"/>).</summary>
	private double NextRow(double y, bool ruled, List<double> rules)
	{
		if (!ruled)
		{
			return y;
		}

		rules.Add(y + RowPadding);
		return y + RowGap;
	}

	private double ColumnsRowHeight(int row) =>
		Math.Max(Cell(row, 0).DesiredSize.Height, Math.Max(Cell(row, 1).DesiredSize.Height, Cell(row, 2).DesiredSize.Height));

	private double StackedRowHeight(int row)
	{
		double[] heights = [.. Enumerable.Range(0, 3).Select(column => Cell(row, column).DesiredSize.Height).Where(height => height > 0)];

		return heights.Sum() + (Math.Max(0, heights.Length - 1) * StackSpacing);
	}
}

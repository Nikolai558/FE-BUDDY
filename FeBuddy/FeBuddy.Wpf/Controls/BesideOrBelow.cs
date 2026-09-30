using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Lays out two children: the second beside the first, on the right, when the row has room for
/// both; otherwise under it. A File Names row uses it for a file and its new-name box.
/// <code>
/// &lt;ctl:BesideOrBelow SideWidth="440" MainMinWidth="300"&gt;
///     &lt;TextBlock Text="{Binding FileName}" /&gt;
///     &lt;TextBox Text="{Binding NewName}" /&gt;
/// &lt;/ctl:BesideOrBelow&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// The choice is made from the width the panel is given, never from what it holds, so a row cannot
/// flip back and forth while it is laid out. Beside, the second child is <see cref="SideWidth"/>
/// wide against the right edge, so the second children of rows one under another line up.
/// </para>
/// <para>
/// A second child with nothing to show - collapsed, or with no height - leaves the first the whole
/// width. Only the first two children are laid out.
/// </para>
/// </remarks>
public sealed class BesideOrBelow : Panel
{
	/// <summary>Identifies the <see cref="SideWidth"/> dependency property.</summary>
	public static readonly DependencyProperty SideWidthProperty = DependencyProperty.Register(
		nameof(SideWidth), typeof(double), typeof(BesideOrBelow),
		new FrameworkPropertyMetadata(400.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="MainMinWidth"/> dependency property.</summary>
	public static readonly DependencyProperty MainMinWidthProperty = DependencyProperty.Register(
		nameof(MainMinWidth), typeof(double), typeof(BesideOrBelow),
		new FrameworkPropertyMetadata(300.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="HorizontalSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(
		nameof(HorizontalSpacing), typeof(double), typeof(BesideOrBelow),
		new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>Identifies the <see cref="VerticalSpacing"/> dependency property.</summary>
	public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(
		nameof(VerticalSpacing), typeof(double), typeof(BesideOrBelow),
		new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

	/// <summary>How wide the second child is when it sits beside the first.</summary>
	public double SideWidth
	{
		get => (double)GetValue(SideWidthProperty);
		set => SetValue(SideWidthProperty, value);
	}

	/// <summary>The least width the first child keeps beside the second; with less, the second goes under it.</summary>
	public double MainMinWidth
	{
		get => (double)GetValue(MainMinWidthProperty);
		set => SetValue(MainMinWidthProperty, value);
	}

	/// <summary>The gap between the two children when they sit side by side.</summary>
	public double HorizontalSpacing
	{
		get => (double)GetValue(HorizontalSpacingProperty);
		set => SetValue(HorizontalSpacingProperty, value);
	}

	/// <summary>The gap between the two children when one is under the other.</summary>
	public double VerticalSpacing
	{
		get => (double)GetValue(VerticalSpacingProperty);
		set => SetValue(VerticalSpacingProperty, value);
	}

	/// <summary>Whether the last layout put the second child beside the first.</summary>
	internal bool IsBeside { get; private set; }

	/// <inheritdoc />
	protected override Size MeasureOverride(Size availableSize)
	{
		(UIElement? main, UIElement? side) = (ChildAt(0), ChildAt(1));
		double width = availableSize.Width;

		for (int i = 2; i < InternalChildren.Count; i++)
		{
			InternalChildren[i].Measure(default);
		}

		bool roomBeside = !double.IsInfinity(width) && width >= MainMinWidth + HorizontalSpacing + SideWidth;

		side?.Measure(new Size(roomBeside ? SideWidth : width, double.PositiveInfinity));
		IsBeside = roomBeside && HasContent(side);

		if (IsBeside)
		{
			main?.Measure(new Size(width - SideWidth - HorizontalSpacing, double.PositiveInfinity));
			Size mainSize = main?.DesiredSize ?? default;

			return new Size(mainSize.Width + HorizontalSpacing + SideWidth, Math.Max(mainSize.Height, side!.DesiredSize.Height));
		}

		main?.Measure(new Size(width, double.PositiveInfinity));
		Size first = main?.DesiredSize ?? default;

		if (!HasContent(side))
		{
			return first;
		}

		return new Size(Math.Max(first.Width, side!.DesiredSize.Width), first.Height + VerticalSpacing + side.DesiredSize.Height);
	}

	/// <inheritdoc />
	protected override Size ArrangeOverride(Size finalSize)
	{
		(UIElement? main, UIElement? side) = (ChildAt(0), ChildAt(1));

		if (IsBeside)
		{
			double mainWidth = Math.Max(0, finalSize.Width - SideWidth - HorizontalSpacing);
			main?.Arrange(new Rect(0, 0, mainWidth, main.DesiredSize.Height));
			side!.Arrange(new Rect(finalSize.Width - SideWidth, 0, SideWidth, side.DesiredSize.Height));
		}
		else
		{
			double mainHeight = main?.DesiredSize.Height ?? 0;
			main?.Arrange(new Rect(0, 0, finalSize.Width, mainHeight));
			side?.Arrange(HasContent(side)
				? new Rect(0, mainHeight + VerticalSpacing, finalSize.Width, side.DesiredSize.Height)
				: new Rect(0, mainHeight, finalSize.Width, 0));
		}

		for (int i = 2; i < InternalChildren.Count; i++)
		{
			InternalChildren[i].Arrange(new Rect());
		}

		return finalSize;
	}

	private UIElement? ChildAt(int index) => index < InternalChildren.Count ? InternalChildren[index] : null;

	private static bool HasContent(UIElement? child) =>
		child is { Visibility: not Visibility.Collapsed } && child.DesiredSize.Height > 0;
}

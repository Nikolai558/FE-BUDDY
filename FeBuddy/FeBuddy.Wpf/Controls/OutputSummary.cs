using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A sub-service's "What You'll Get" summary (<see cref="SummaryBlock"/>): each block headed by its
/// outputs' tags, then the filters that pick what they get, one per line, each after the first
/// starting with a chip (<c>for</c>, <c>and only</c>, <c>along with</c>, ...) so the block reads as
/// one sentence, then any "Outputs include" lines.
/// Shown by the What You'll Get card at the top of each sub-service tab and by that tab's section
/// on the Preview Settings tab; its look is in Theme/Controls.Surfaces.xaml.
/// <code>
/// &lt;ctl:OutputSummary Blocks="{Binding WhatYoullGet}" /&gt;
/// </code>
/// </summary>
public sealed class OutputSummary : Control
{
	/// <summary>Identifies the <see cref="Blocks"/> dependency property.</summary>
	public static readonly DependencyProperty BlocksProperty = DependencyProperty.Register(
		nameof(Blocks), typeof(IReadOnlyList<SummaryBlock>), typeof(OutputSummary),
		new PropertyMetadata(null, (d, _) => ((OutputSummary)d).Refresh()));

	private static readonly DependencyPropertyKey ItemsKey = DependencyProperty.RegisterReadOnly(
		nameof(Items), typeof(IReadOnlyList<OutputSummaryItem>), typeof(OutputSummary),
		new PropertyMetadata(Array.Empty<OutputSummaryItem>()));

	/// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
	public static readonly DependencyProperty ItemsProperty = ItemsKey.DependencyProperty;

	/// <summary>The summary's blocks, as the tab builds them.</summary>
	public IReadOnlyList<SummaryBlock>? Blocks
	{
		get => (IReadOnlyList<SummaryBlock>?)GetValue(BlocksProperty);
		set => SetValue(BlocksProperty, value);
	}

	/// <summary>The blocks as drawn: each with its outputs' tags, its lines, and its "Outputs include" lines.</summary>
	public IReadOnlyList<OutputSummaryItem> Items => (IReadOnlyList<OutputSummaryItem>)GetValue(ItemsProperty);

	private void Refresh()
	{
		List<OutputSummaryItem> items = [.. (Blocks ?? []).Select(block =>
			new OutputSummaryItem(OutputTag.For(block.Outputs, block.Outputs), block.Lines, block.Includes))];

		SetValue(ItemsKey, items);
	}
}

/// <summary>One block of an <see cref="OutputSummary"/> as drawn.</summary>
/// <param name="Tags">Its outputs' tags, which head it.</param>
/// <param name="Lines">Its lines.</param>
/// <param name="Includes">Its "Outputs include" lines; none for most blocks.</param>
public sealed record OutputSummaryItem(IReadOnlyList<OutputTag> Tags, IReadOnlyList<SummaryLine> Lines, IReadOnlyList<SummaryLine> Includes)
{
	/// <summary>Whether the block has "Outputs include" lines.</summary>
	public bool HasIncludes => Includes.Count > 0;
}

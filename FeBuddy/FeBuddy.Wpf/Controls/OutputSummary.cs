using System.Windows;
using System.Windows.Controls;

using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A sub-service's "What You'll Get" summary (<see cref="SummaryBlock"/>): the filters that pick
/// what each output gets, one per line, with AND, OR or PLUS in the margin. Where the outputs get
/// different things, each block starts with its outputs' tags. Shown by the What You'll Get card at
/// the top of each sub-service tab and by that tab's section on the Preview Settings tab; its look
/// is in Theme/Controls.Surfaces.xaml.
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

	/// <summary>The blocks as drawn: each with its outputs' tags when there is more than one block, and its lines.</summary>
	public IReadOnlyList<OutputSummaryItem> Items => (IReadOnlyList<OutputSummaryItem>)GetValue(ItemsProperty);

	private void Refresh()
	{
		IReadOnlyList<SummaryBlock> blocks = Blocks ?? [];
		bool tagged = blocks.Count > 1;

		List<OutputSummaryItem> items = [.. blocks.Select(block =>
			new OutputSummaryItem(tagged ? OutputTag.For(block.Outputs, block.Outputs) : [], block.Lines))];

		SetValue(ItemsKey, items);
	}
}

/// <summary>One block of an <see cref="OutputSummary"/> as drawn.</summary>
/// <param name="Tags">Its outputs' tags; none when it is the only block (the card's own tags say then).</param>
/// <param name="Lines">Its lines.</param>
public sealed record OutputSummaryItem(IReadOnlyList<OutputTag> Tags, IReadOnlyList<SummaryLine> Lines);

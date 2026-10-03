using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;

using FeBuddy.Wpf.Behaviors;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Shows the FE-Buddy Alias Command Guide (<see cref="AliasGuideContent"/>) in the app the way the
/// exported web page shows it: one card per section, each command's syntax, description and
/// examples, and every part a controller replaces coloured by its kind.
/// <code>
/// &lt;ctl:AliasGuideDocumentView Document="{Binding Document}" /&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Built in code from the guide's blocks, like <see cref="MarkdownView"/>, and themed through
/// resource references only: a part takes <c>Brush.Command.&lt;Kind&gt;</c> and its <c>.Soft</c> fill
/// from Palette.xaml. In a command's syntax, its examples, the colour key and a table cell that is
/// just a command, each part is a pill with a <c>.Line</c> edge (dashed when the part is optional);
/// in running text it is a run on the soft fill instead, which keeps it on the sentence's baseline.
/// </para>
/// <para>
/// A command's examples sit beside its syntax and description when the card is wide enough
/// (<see cref="BesideOrBelow"/>), and under them when it is not; a long command wraps between its
/// parts, never inside one.
/// </para>
/// </remarks>
public sealed class AliasGuideDocumentView : Decorator
{
	/// <summary>Identifies the <see cref="Document"/> dependency property.</summary>
	public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
		nameof(Document), typeof(AliasGuideDocument), typeof(AliasGuideDocumentView),
		new PropertyMetadata(null, (d, _) => ((AliasGuideDocumentView)d).Rebuild()));

	/// <summary>The font size of a command in running text, a table or an example.</summary>
	private const double CommandSize = 12.5;

	/// <summary>The font size of a command's syntax, at the head of its row.</summary>
	private const double SyntaxSize = 13.5;

	/// <summary>The guide to show.</summary>
	public AliasGuideDocument? Document
	{
		get => (AliasGuideDocument?)GetValue(DocumentProperty);
		set => SetValue(DocumentProperty, value);
	}

	private void Rebuild()
	{
		if (Document is not { } guide)
		{
			Child = null;
			return;
		}

		StackPanel root = new();

		TextBlock lead = Prose(guide.Lead, "Text.Prose");
		lead.Margin = new Thickness(0, 0, 0, 14);
		root.Children.Add(lead);

		root.Children.Add(SectionCard(guide.About));
		root.Children.Add(NotationCard(guide.ReadingNotes));

		foreach (GuideSection section in guide.Sections)
		{
			root.Children.Add(SectionCard(section));
		}

		Child = root;
	}

	// ------------------------------------------------------------------ cards

	private static Card SectionCard(GuideSection section)
	{
		StackPanel body = new();

		if (section.Intro is { } intro)
		{
			TextBlock text = Prose(intro, "Text.Prose");
			text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Tertiary");
			body.Children.Add(text);
		}

		GuideBlock? previous = null;

		foreach (GuideBlock block in section.Blocks)
		{
			FrameworkElement element = block switch
			{
				GuideHeading heading => Heading(heading.Text),
				GuideList list => BulletList(list),
				GuideCommandTable commands => CommandTable(commands),
				GuideTable table => Table(table),
				GuideParagraph paragraph => Prose(paragraph.Text, "Text.Prose"),
				_ => new Border(),
			};

			// A heading has air above it; whatever follows a heading hugs it.
			if (block is not GuideHeading && body.Children.Count > 0)
			{
				element.Margin = new Thickness(0, previous is GuideHeading ? 0 : 12, 0, 0);
			}

			body.Children.Add(element);
			previous = block;
		}

		return new Card { Header = section.Title, Content = body };
	}

	/// <summary>"How to read this guide": what plain text, a pill and a dashed pill mean, the colour key, then the notes.</summary>
	private static Card NotationCard(IReadOnlyList<string> notes)
	{
		StackPanel body = new();

		body.Children.Add(LegendRow(CommandLine([new CommandPart(".apt", CommandPartKind.Typed, false, false)], CommandSize, wrap: false), AliasGuideContent.TypedLegend));
		body.Children.Add(LegendRow(Pill(new CommandPart("Highlighted", CommandPartKind.Identifier, true, false), CommandSize), AliasGuideContent.PlaceholderLegend));

		WrapPanel key = new() { Margin = new Thickness(0, 2, 0, 4) };

		foreach (CommandPartKind kind in AliasGuideContent.KeyKinds)
		{
			FrameworkElement pill = Pill(new CommandPart(AliasGuideContent.KindLabel(kind), kind, true, false), CommandSize);
			pill.Margin = new Thickness(0, 4, 8, 4);
			key.Children.Add(pill);
		}

		body.Children.Add(key);
		body.Children.Add(LegendRow(Pill(new CommandPart("Dashed", CommandPartKind.Page, true, true), CommandSize), AliasGuideContent.OptionalLegend));

		foreach (string note in notes)
		{
			TextBlock text = Prose(note, "Text.Prose");
			text.Margin = new Thickness(0, 10, 0, 0);
			body.Children.Add(text);
		}

		return new Card { Header = AliasGuideContent.NotationTitle, Content = body };
	}

	private static DockPanel LegendRow(FrameworkElement sample, string meaning)
	{
		DockPanel row = new() { Margin = new Thickness(0, 4, 0, 4) };
		sample.Margin = new Thickness(0, 0, 10, 0);
		sample.VerticalAlignment = VerticalAlignment.Center;
		DockPanel.SetDock(sample, Dock.Left);
		row.Children.Add(sample);

		TextBlock text = new() { Text = meaning, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
		text.SetResourceReference(StyleProperty, "Text.Body");
		row.Children.Add(text);
		return row;
	}

	// ----------------------------------------------------------------- blocks

	private static SectionHeader Heading(string text)
	{
		SectionHeader heading = new() { Label = text, Margin = new Thickness(0, 20, 0, 8) };
		heading.SetResourceReference(StyleProperty, "SectionHeader.Group");
		return heading;
	}

	private static StackPanel BulletList(GuideList list)
	{
		StackPanel panel = new();

		foreach (string item in list.Items)
		{
			DockPanel row = new() { Margin = new Thickness(0, 0, 0, 6) };

			TextBlock bullet = new() { Text = "•", Margin = new Thickness(2, 0, 10, 0) };
			bullet.SetResourceReference(StyleProperty, "Text.Body");
			bullet.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Tertiary");
			DockPanel.SetDock(bullet, Dock.Left);
			row.Children.Add(bullet);

			row.Children.Add(Prose(item, "Text.Prose"));
			panel.Children.Add(row);
		}

		return panel;
	}

	/// <summary>One row per command: its syntax and description, its notes, and its examples beside or under them.</summary>
	private static StackPanel CommandTable(GuideCommandTable table)
	{
		StackPanel panel = new();

		for (int i = 0; i < table.Commands.Count; i++)
		{
			GuideCommand command = table.Commands[i];

			if (i > 0)
			{
				panel.Children.Add(Divider(new Thickness(0, 14, 0, 14)));
			}

			StackPanel main = new();
			main.Children.Add(CommandLine(CommandMarkup.Parse(command.Syntax), SyntaxSize, wrap: true));

			TextBlock description = Prose(command.Description, "Text.Body");
			description.Margin = new Thickness(0, 8, 0, 0);
			main.Children.Add(description);

			if (command.Notes.Count > 0)
			{
				WrapPanel notes = new();

				foreach (string note in command.Notes)
				{
					notes.Children.Add(Note(note));
				}

				main.Children.Add(notes);
			}

			StackPanel side = new();
			TextBlock label = new() { Text = command.Examples.Count == 1 ? "EXAMPLE" : "EXAMPLES" };
			label.SetResourceReference(StyleProperty, "Text.Label");
			side.Children.Add(label);

			foreach (string example in command.Examples)
			{
				FrameworkElement line = CommandLine(CommandMarkup.Parse(example), CommandSize, wrap: true);
				line.Margin = new Thickness(0, 6, 0, 0);
				side.Children.Add(line);
			}

			panel.Children.Add(new BesideOrBelow
			{
				SideWidth = 250,
				MainMinWidth = 320,
				HorizontalSpacing = 28,
				VerticalSpacing = 12,
				Children = { main, side },
			});
		}

		return panel;
	}

	/// <summary>
	/// A plain table: a heading row, then a row per entry, each under a rule. The last column (the
	/// commands) is as wide as its widest cell in every row; the others share the rest.
	/// </summary>
	private static StackPanel Table(GuideTable table)
	{
		StackPanel panel = new();
		Grid.SetIsSharedSizeScope(panel, true);

		panel.Children.Add(TableRow(table.Headers.Count, [.. table.Headers.Select(HeaderCell)]));

		foreach (IReadOnlyList<string> row in table.Rows)
		{
			panel.Children.Add(Divider(new Thickness(0)));
			panel.Children.Add(TableRow(table.Headers.Count, [.. row.Select(Cell)]));
		}

		return panel;
	}

	/// <summary>A table cell: one that is just a command shows it as pills, like a command's examples; anything else is text.</summary>
	private static UIElement Cell(string text) =>
		GuideInline.Parse(text) is [{ Style: InlineStyle.Code } only] && only.Parts.Any(part => part.Kind != CommandPartKind.Typed)
			? CommandLine(only.Parts, CommandSize, wrap: true)
			: Prose(text, "Text.Body");

	private static TextBlock HeaderCell(string header)
	{
		TextBlock text = new() { Text = header.ToUpperInvariant() };
		text.SetResourceReference(StyleProperty, "Text.Label");
		return text;
	}

	private static Grid TableRow(int columns, IReadOnlyList<UIElement> cells)
	{
		Grid grid = new();

		for (int i = 0; i < columns; i++)
		{
			grid.ColumnDefinitions.Add(i < columns - 1
				? new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
				: new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "LastColumn" });
		}

		for (int i = 0; i < cells.Count && i < columns; i++)
		{
			if (cells[i] is FrameworkElement cell)
			{
				cell.Margin = new Thickness(0, 8, i < columns - 1 ? 16 : 0, 8);
				cell.VerticalAlignment = VerticalAlignment.Center;
			}

			Grid.SetColumn(cells[i], i);
			grid.Children.Add(cells[i]);
		}

		return grid;
	}

	// ---------------------------------------------------------------- inlines

	/// <summary>Inline text in a TextBlock: plain, bold, and code - a command coloured part by part.</summary>
	private static TextBlock Prose(string text, string styleKey)
	{
		TextBlock block = new() { TextWrapping = TextWrapping.Wrap };
		block.SetResourceReference(StyleProperty, styleKey);

		foreach (InlineRun run in GuideInline.Parse(text))
		{
			block.Inlines.Add(run.Style switch
			{
				InlineStyle.Bold => Bold(run.Text),
				InlineStyle.Code when run.Parts.Any(part => part.Kind != CommandPartKind.Typed) => InlineCommand(run.Parts),
				InlineStyle.Code => Code(run.Text),
				_ => new Run(run.Text),
			});
		}

		return block;
	}

	/// <summary>
	/// A command in running text, as runs rather than pills, so it shares the sentence's baseline
	/// exactly: typed text plain, every other part in its kind's colour on its soft fill.
	/// </summary>
	private static Span InlineCommand(IReadOnlyList<CommandPart> parts)
	{
		Span command = new();
		command.SetResourceReference(TextElement.FontFamilyProperty, "Font.Mono");

		foreach (CommandPart part in parts)
		{
			Run run = new(part.Text) { FontSize = CommandSize };

			if (part.Kind == CommandPartKind.Typed)
			{
				run.SetResourceReference(TextElement.ForegroundProperty, "Brush.Text.Primary");
			}
			else
			{
				string brush = "Brush.Command." + part.Kind;
				run.SetResourceReference(TextElement.ForegroundProperty, brush);
				run.SetResourceReference(TextElement.BackgroundProperty, brush + ".Soft");
			}

			command.Inlines.Add(run);
		}

		return command;
	}

	private static Run Bold(string text)
	{
		Run run = new(text) { FontWeight = FontWeights.SemiBold };
		run.SetResourceReference(TextElement.ForegroundProperty, "Brush.Text.Primary");
		return run;
	}

	private static Run Code(string text)
	{
		Run run = new(text) { FontSize = CommandSize };
		InlineCode.ApplyLook(run);
		return run;
	}

	/// <summary>A command's parts side by side: typed text as it is, every other part a pill.</summary>
	private static FrameworkElement CommandLine(IReadOnlyList<CommandPart> parts, double size, bool wrap)
	{
		Panel line = wrap ? new WrapPanel() : new StackPanel { Orientation = Orientation.Horizontal };

		foreach (CommandPart part in parts)
		{
			line.Children.Add(part.Kind == CommandPartKind.Typed ? Typed(part.Text, size) : Pill(part, size));
		}

		return line;
	}

	private static TextBlock Typed(string text, double size)
	{
		TextBlock block = new() { Text = text, FontSize = size, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 1, 0, 1) };
		block.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Mono");
		block.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Primary");
		return block;
	}

	/// <summary>
	/// A part to replace: its text in its kind's colour, on a soft fill, with an edge that is dashed
	/// when the part is optional. Its tooltip names the kind.
	/// </summary>
	private static FrameworkElement Pill(CommandPart part, double size)
	{
		string brush = "Brush.Command." + part.Kind;

		Rectangle edge = new() { RadiusX = 5, RadiusY = 5, StrokeThickness = 1, SnapsToDevicePixels = true };
		edge.SetResourceReference(Shape.StrokeProperty, brush + ".Line");
		edge.SetResourceReference(Shape.FillProperty, brush + ".Soft");

		if (part.IsOptional)
		{
			edge.StrokeDashArray = [3, 2];
		}

		TextBlock text = new() { Text = part.Text, FontSize = size, Margin = new Thickness(5, 1, 5, 1), VerticalAlignment = VerticalAlignment.Center };
		text.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Mono");
		text.SetResourceReference(TextBlock.ForegroundProperty, brush);

		return new Grid
		{
			Margin = new Thickness(1),
			VerticalAlignment = VerticalAlignment.Center,
			ToolTip = AliasGuideContent.KindLabel(part.Kind) + (part.IsOptional ? " (optional)" : string.Empty),
			Children = { edge, text },
		};
	}

	/// <summary>A short note under a command's description, in a hairline-edged chip.</summary>
	private static Border Note(string text)
	{
		TextBlock content = Prose(text, "Text.Caption");

		Border chip = new()
		{
			Child = content,
			BorderThickness = new Thickness(1),
			Padding = new Thickness(8, 2, 8, 3),
			Margin = new Thickness(0, 8, 6, 0),
		};
		chip.SetResourceReference(Border.BorderBrushProperty, "Brush.Stroke.Strong");
		chip.SetResourceReference(Border.CornerRadiusProperty, "Radius.Chip");
		return chip;
	}

	private static Border Divider(Thickness margin)
	{
		Border rule = new() { Margin = margin };
		rule.SetResourceReference(StyleProperty, "Divider");
		return rule;
	}
}

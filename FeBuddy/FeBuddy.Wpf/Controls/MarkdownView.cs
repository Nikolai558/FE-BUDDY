using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using FeBuddy.Core.Models.General;
using FeBuddy.Core.Parsers.Markdown;
using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// Renders Markdown (parsed by <see cref="MarkdownParser"/>) as themed WPF text: headings,
/// paragraphs, nested bullet / numbered lists, code, quotes and rules, with clickable links
/// that open in the browser. Built from plain TextBlocks so it sizes to its content and picks
/// up the app's type ramp (Text.Body, Font.Display, ...) - no FlowDocument, no NuGet.
/// <code>
/// &lt;ctl:MarkdownView Markdown="{Binding ReleaseNotes}"
///                   IssueUrlBase="https://github.com/owner/repo/issues/" /&gt;
/// </code>
/// </summary>
public sealed class MarkdownView : Decorator
{
    public static readonly DependencyProperty MarkdownProperty = DependencyProperty.Register(
        nameof(Markdown), typeof(string), typeof(MarkdownView),
        new PropertyMetadata(null, (d, _) => ((MarkdownView)d).Rebuild()));

    public static readonly DependencyProperty IssueUrlBaseProperty = DependencyProperty.Register(
        nameof(IssueUrlBase), typeof(string), typeof(MarkdownView),
        new PropertyMetadata(null, (d, _) => ((MarkdownView)d).Rebuild()));

    public static readonly DependencyProperty HeadingOffsetProperty = DependencyProperty.Register(
        nameof(HeadingOffset), typeof(int), typeof(MarkdownView),
        new PropertyMetadata(0, (d, _) => ((MarkdownView)d).Rebuild()));

    // Bullet glyph per nesting depth, cycling after the third level.
    private static readonly string[] Bullets = ["•", "◦", "▪"];

    /// <summary>The Markdown text to show.</summary>
    public string? Markdown
    {
        get => (string?)GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    /// <summary>When set, <c>#123</c> references link to this base plus the number (a GitHub issues URL ending in <c>/</c>).</summary>
    public string? IssueUrlBase
    {
        get => (string?)GetValue(IssueUrlBaseProperty);
        set => SetValue(IssueUrlBaseProperty, value);
    }

    /// <summary>
    /// Levels to push every heading down by (capped at level 6), for Markdown shown under a
    /// heading of its own - the update window's per-release sections use 1, so a release's
    /// "## Change log" sits below its version header.
    /// </summary>
    public int HeadingOffset
    {
        get => (int)GetValue(HeadingOffsetProperty);
        set => SetValue(HeadingOffsetProperty, value);
    }

    private void Rebuild()
    {
        var root = new StackPanel();
        AddBlocks(root, Demote(MarkdownParser.Parse(Markdown, IssueUrlBase), HeadingOffset), 0);
        Child = root;
    }

    private static IReadOnlyList<MarkdownBlock> Demote(IReadOnlyList<MarkdownBlock> blocks, int offset)
    {
        if (offset <= 0)
        {
            return blocks;
        }

        return blocks.Select(block => block switch
        {
            MarkdownHeading heading => heading with { Level = Math.Min(6, heading.Level + offset) },
            MarkdownList list => list with { Items = list.Items.Select(item => new MarkdownListItem(Demote(item.Blocks, offset))).ToList() },
            MarkdownQuote quote => quote with { Blocks = Demote(quote.Blocks, offset) },
            _ => block,
        }).ToList();
    }

    private static void AddBlocks(Panel panel, IReadOnlyList<MarkdownBlock> blocks, int listDepth)
    {
        for (int i = 0; i < blocks.Count; i++)
        {
            MarkdownBlock block = blocks[i];
            FrameworkElement element = block switch
            {
                MarkdownHeading heading => BuildHeading(heading),
                MarkdownParagraph paragraph => BuildText(paragraph.Spans, "Text.Body"),
                MarkdownList list => BuildList(list, listDepth),
                MarkdownCodeBlock code => BuildCode(code),
                MarkdownQuote quote => BuildQuote(quote, listDepth),
                _ => BuildRule(),
            };

            if (i > 0)
            {
                element.Margin = new Thickness(0, SpaceBefore(block, blocks[i - 1], listDepth), 0, element.Margin.Bottom);
            }

            panel.Children.Add(element);
        }
    }

    // Headings get air above them; a nested list hugs the item text it hangs off.
    private static double SpaceBefore(MarkdownBlock block, MarkdownBlock previous, int listDepth) => block switch
    {
        MarkdownHeading => 16,
        MarkdownList when listDepth > 0 => 4,
        _ when previous is MarkdownHeading => 6,
        _ => 10,
    };

    private static TextBlock BuildHeading(MarkdownHeading heading)
    {
        TextBlock text = BuildText(heading.Spans, "Text.Body.Strong");
        text.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Display");
        text.FontSize = heading.Level switch
        {
            1 => 17,
            2 => 15,
            3 => 13.5,
            _ => 13,
        };
        text.FontWeight = heading.Level == 1 ? FontWeights.Bold : FontWeights.SemiBold;
        text.LineHeight = double.NaN;
        return text;
    }

    private static StackPanel BuildList(MarkdownList list, int listDepth)
    {
        var panel = new StackPanel();

        for (int i = 0; i < list.Items.Count; i++)
        {
            var row = new Grid { Margin = new Thickness(0, i == 0 ? 0 : 4, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            string marker = list.Ordered
                ? (list.Start + i).ToString(CultureInfo.CurrentCulture) + "."
                : Bullets[listDepth % Bullets.Length];

            var markerText = new TextBlock
            {
                Text = marker,
                MinWidth = list.Ordered ? 20 : 16,
                Margin = new Thickness(0, 0, 4, 0),
            };
            markerText.SetResourceReference(StyleProperty, "Text.Body");
            markerText.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Tertiary");
            row.Children.Add(markerText);

            var content = new StackPanel();
            AddBlocks(content, list.Items[i].Blocks, listDepth + 1);
            Grid.SetColumn(content, 1);
            row.Children.Add(content);

            panel.Children.Add(row);
        }

        return panel;
    }

    private static Border BuildCode(MarkdownCodeBlock code)
    {
        var text = new TextBlock
        {
            Text = code.Text,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };
        text.SetResourceReference(TextBlock.FontFamilyProperty, "Font.Mono");
        text.SetResourceReference(TextBlock.ForegroundProperty, "Brush.Text.Primary");

        var border = new Border
        {
            Child = text,
            Padding = new Thickness(10, 8, 10, 8),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
        };
        border.SetResourceReference(Border.BackgroundProperty, "Brush.Card");
        border.SetResourceReference(Border.BorderBrushProperty, "Brush.Stroke");
        return border;
    }

    private static Border BuildQuote(MarkdownQuote quote, int listDepth)
    {
        var content = new StackPanel();
        AddBlocks(content, quote.Blocks, listDepth);

        var border = new Border
        {
            Child = content,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 0, 0, 0),
        };
        border.SetResourceReference(Border.BorderBrushProperty, "Brush.Stroke.Strong");
        return border;
    }

    private static Border BuildRule()
    {
        var rule = new Border();
        rule.SetResourceReference(StyleProperty, "Divider");
        return rule;
    }

    private static TextBlock BuildText(IReadOnlyList<MarkdownSpan> spans, string styleKey)
    {
        var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
        text.SetResourceReference(StyleProperty, styleKey);

        foreach (MarkdownSpan span in spans)
        {
            text.Inlines.Add(BuildInline(span));
        }

        return text;
    }

    private static Inline BuildInline(MarkdownSpan span)
    {
        // Hard line breaks arrive as \n inside the span's text.
        var styled = new Span();
        string[] lines = span.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
            {
                styled.Inlines.Add(new LineBreak());
            }

            styled.Inlines.Add(new Run(lines[i]));
        }

        if (span.Style.HasFlag(MarkdownStyle.Bold))
        {
            styled.FontWeight = FontWeights.SemiBold;
            if (span.Url is null)
            {
                styled.SetResourceReference(TextElement.ForegroundProperty, "Brush.Text.Primary");
            }
        }

        if (span.Style.HasFlag(MarkdownStyle.Italic))
        {
            styled.FontStyle = FontStyles.Italic;
        }

        if (span.Style.HasFlag(MarkdownStyle.Strikethrough))
        {
            styled.TextDecorations = TextDecorations.Strikethrough;
        }

        if (span.Style.HasFlag(MarkdownStyle.Code))
        {
            styled.FontSize = 12;
            styled.SetResourceReference(TextElement.FontFamilyProperty, "Font.Mono");
            styled.SetResourceReference(TextElement.BackgroundProperty, "Brush.Stroke");
            if (span.Url is null)
            {
                styled.SetResourceReference(TextElement.ForegroundProperty, "Brush.Text.Primary");
            }
        }

        if (span.Url is null)
        {
            return styled;
        }

        string url = span.Url;
        var link = new Hyperlink(styled) { ToolTip = url };
        link.SetResourceReference(TextElement.ForegroundProperty, "Brush.Accent");
        link.Click += (_, _) => BrowserLauncher.Open(url);
        return link;
    }
}

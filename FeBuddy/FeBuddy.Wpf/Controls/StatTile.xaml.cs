using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>One KPI tile: icon, value, caption. See StatTile.xaml.</summary>
public partial class StatTile : UserControl
{
    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(StatTile), new PropertyMetadata(string.Empty));

    public StatTile() => InitializeComponent();

    /// <summary>Icon glyph string (from Icons.xaml).</summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>The headline number / short value.</summary>
    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>One-line description under the value.</summary>
    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }
}

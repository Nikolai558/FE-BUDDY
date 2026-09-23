using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>Small uppercase heading for a content block. See SectionHeader.xaml.</summary>
public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(SectionHeader), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty AsideProperty = DependencyProperty.Register(
        nameof(Aside), typeof(object), typeof(SectionHeader), new PropertyMetadata(null));

    public SectionHeader() => InitializeComponent();

    /// <summary>The heading text, in normal case; it is shown upper-cased.</summary>
    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>Optional element shown right-aligned next to the label (e.g. a link button).</summary>
    public object? Aside
    {
        get => GetValue(AsideProperty);
        set => SetValue(AsideProperty, value);
    }
}

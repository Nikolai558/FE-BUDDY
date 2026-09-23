using System.Windows;
using System.Windows.Controls;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The standard rounded panel every page is built from, with an optional title. Its whole look
/// (background, border, corner, padding, the space below it, the title) comes from the implicit
/// Card style in Theme/Controls.Surfaces.xaml; a card that must look different uses one of the
/// named variants there (Card.Warn, Card.Danger, Card.Popup) rather than local overrides.
/// <code>
/// &lt;ctl:Card Header="Outputs"&gt;
///     ...controls...
/// &lt;/ctl:Card&gt;
/// </code>
/// </summary>
public sealed class Card : ContentControl
{
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(string), typeof(Card), new PropertyMetadata(null));

    /// <summary>
    /// The card's title, written in normal case ("Region of Interest"); SectionHeader sets the
    /// casing. Leave unset for a card with no title.
    /// </summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}

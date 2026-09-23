using FeBuddy.Wpf.Infrastructure;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One entry in the side navigation. Owns the view-model it points at and
/// creates it lazily the first time the user opens that section, so switching
/// tabs keeps its state.
/// <para>
/// Selection is modelled as <see cref="IsActive"/> so the nav rows can be plain
/// grouped RadioButtons - that keeps a single highlight across the two separate
/// nav groups without any ListBox selection juggling.
/// </para>
/// </summary>
public sealed class NavItem : ObservableObject
{
    private readonly Func<object> _viewModelFactory;
    private readonly Action<NavItem> _onActivated;
    private object? _viewModel;
    private bool _isActive;

    /// <summary>Creates a nav entry.</summary>
    /// <param name="title">The row's label.</param>
    /// <param name="glyph">Icon glyph from <c>Icons.xaml</c>.</param>
    /// <param name="viewModelFactory">Builds the section's view-model on first activation.</param>
    /// <param name="onActivated">Called when this row becomes the active one.</param>
    public NavItem(
        string title,
        string glyph,
        Func<object> viewModelFactory,
        Action<NavItem> onActivated)
    {
        Title = title;
        Glyph = glyph;
        _viewModelFactory = viewModelFactory;
        _onActivated = onActivated;
    }

    public string Title { get; }

    /// <summary>Icon glyph string (from Icons.xaml).</summary>
    public string Glyph { get; }

    /// <summary>The section's view-model; built on first access, cached after.</summary>
    public object ViewModel => _viewModel ??= _viewModelFactory();

    /// <summary>True when this is the section on screen. Bound two-way to the nav RadioButton.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value) && value)
            {
                _onActivated(this);
            }
        }
    }
}

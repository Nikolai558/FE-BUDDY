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

    public NavItem(
        string title,
        string glyph,
        Func<object> viewModelFactory,
        Action<NavItem> onActivated,
        IReadOnlyList<NavItem>? subServices = null)
    {
        Title = title;
        Glyph = glyph;
        _viewModelFactory = viewModelFactory;
        _onActivated = onActivated;
        SubServices = subServices ?? Array.Empty<NavItem>();
    }

    public string Title { get; }

    /// <summary>Icon glyph string (from Icons.xaml).</summary>
    public string Glyph { get; }

    /// <summary>
    /// This service's sub-services, in order. Empty for a leaf service (Dashboard, Map). Only
    /// AIRAC Service has any today - Airways (rule 1.3). A sub-service page is hosted inside its
    /// parent service's screen, never as a new top-level nav entry (remediation plan 5.2).
    /// </summary>
    public IReadOnlyList<NavItem> SubServices { get; }

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

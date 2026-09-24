namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// One row in the General tab's sub-service picker: the user ticking it means "produce data for
/// this, and give me a tab to configure it".
/// </summary>
/// <param name="descriptor">The catalogue entry this row represents.</param>
/// <param name="isSelected">Whether it starts selected (restored from <c>UserConfig</c>).</param>
/// <param name="onChanged">Raised when the user ticks or unticks the row.</param>
public sealed class SubServiceSelection(SubServiceDescriptor descriptor, bool isSelected, Action onChanged) : ObservableObject
{
	private readonly Action _onChanged = onChanged;
	private bool _isSelected = isSelected;

	/// <summary>The catalogue entry.</summary>
	public SubServiceDescriptor Descriptor { get; } = descriptor;

	/// <summary>The sub-service's stable key.</summary>
	public string Key => Descriptor.Key;

	/// <summary>The name shown to the user.</summary>
	public string DisplayName => Descriptor.DisplayName;

	/// <summary>Whether the backend for this sub-service exists yet.</summary>
	public bool IsImplemented => Descriptor.IsImplemented;

	/// <summary>A short note for a sub-service that has no backend yet, otherwise empty.</summary>
	public string Hint => IsImplemented ? string.Empty : "settings coming soon";

	/// <summary>Whether the user wants this sub-service in the run. Drives its tab existing.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				_onChanged();
			}
		}
	}
}

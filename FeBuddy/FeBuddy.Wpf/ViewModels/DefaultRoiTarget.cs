using FeBuddy.Core.Domain.Geo.Models;
using FeBuddy.Wpf.Shell;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>The Map page's ROI: the one saved default ROI that Settings edits too.</summary>
public sealed class DefaultRoiTarget : IRoiTarget
{
	/// <summary>Creates the target, following the saved default ROI.</summary>
	public DefaultRoiTarget()
	{
		// View-models live for the whole session (NavItem caches them), so the Map page has to
		// hear when Settings changes or clears the default ROI.
		DefaultRoiStore.Changed += (_, _) => CurrentChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	public event EventHandler? CurrentChanged;

	/// <inheritdoc />
	public string Title => "Default Region of Interest";

	/// <inheritdoc />
	public string Description => "Every sub-service uses this box unless it has its own override. Saving here updates Settings too.";

	/// <inheritdoc />
	public string SaveLabel => "Save";

	/// <inheritdoc />
	public bool CanClear => true;

	/// <inheritdoc />
	public bool StartsEditing => false;

	/// <inheritdoc />
	public RegionOfInterest? Current => DefaultRoiStore.Load();

	/// <inheritdoc />
	public RegionOfInterest? Reference => null;

	/// <inheritdoc />
	public void Commit(RegionOfInterest? roi)
	{
		if (roi is null)
		{
			DefaultRoiStore.Clear();
			Toast.Info("Default ROI cleared", "Sub-services without their own override now cover everything.");
		}
		else
		{
			DefaultRoiStore.Set(roi);
			Toast.Success("Default ROI saved", "Settings ▸ Default Region of Interest now uses this box.");
		}
	}

	/// <inheritdoc />
	public void Cancel()
	{
	}
}

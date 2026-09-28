using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// One GeoJSON file from a run's output folder, as listed in the Map page's output picker. Ticking
/// it puts the file on the map; the choice is remembered by its path inside the cycle folder, so
/// the same file from the next cycle's run shows up already ticked.
/// </summary>
/// <param name="file">The file on disk.</param>
/// <param name="isSelected">Whether it starts ticked.</param>
/// <param name="onSelectedChanged">Called when the user ticks or unticks it.</param>
public sealed class OutputFileChoice(AiracOutputGeojsonFile file, bool isSelected, Action<OutputFileChoice> onSelectedChanged) : ObservableObject
{
	private bool _isSelected = isSelected;

	/// <summary>The file on disk.</summary>
	public AiracOutputGeojsonFile File { get; } = file;

	/// <summary>The file name without its extension.</summary>
	public string Name => File.Name;

	/// <summary>The folder group it is listed under, e.g. <c>Upload to vNAS · ZOB\CLE</c>.</summary>
	public string Group => (File.UploadToVnas ? "Upload to vNAS" : "GeoJSON")
		+ (File.SubFolder.Length > 0 ? $"  ·  {File.SubFolder}" : string.Empty);

	/// <summary>Its size, e.g. <c>1.4 MB</c>.</summary>
	public string SizeText => File.SizeBytes switch
	{
		< 1024 => $"{File.SizeBytes} B",
		< 1024 * 1024 => $"{File.SizeBytes / 1024.0:0} KB",
		_ => $"{File.SizeBytes / (1024.0 * 1024.0):0.0} MB",
	};

	/// <summary>Whether the file is on the map.</summary>
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (SetProperty(ref _isSelected, value))
			{
				onSelectedChanged(this);
			}
		}
	}

	/// <summary>Sets <see cref="IsSelected"/> without telling the owner (it made the change).</summary>
	/// <param name="value">The new value.</param>
	public void SetSelectedSilently(bool value) => SetProperty(ref _isSelected, value, nameof(IsSelected));
}

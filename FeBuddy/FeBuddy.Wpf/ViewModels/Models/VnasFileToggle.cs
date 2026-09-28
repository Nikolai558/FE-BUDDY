using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One file on the Upload to vNAS card: whether it goes to vNAS, and - when the CRC-ERAM question
/// is answered "specific files" - whether it gets CRC-ERAM defaults.
/// </summary>
/// <remarks>
/// The owning tab keeps the saved choices and rebuilds these whenever the files it writes
/// change, so a toggle is only ever a view of those choices; <paramref name="onChanged"/> hands
/// each change back to it.
/// </remarks>
/// <param name="file">The file.</param>
/// <param name="isUploaded">Whether it starts marked for vNAS.</param>
/// <param name="hasCrcDefaults">Whether it starts chosen for CRC-ERAM defaults.</param>
/// <param name="onChanged">Called with this toggle when the user ticks or unticks either box.</param>
public sealed class VnasFileToggle(OutputFileOption file, bool isUploaded, bool hasCrcDefaults, Action<VnasFileToggle> onChanged) : ObservableObject
{
	private readonly Action<VnasFileToggle> _onChanged = onChanged;
	private bool _isUploaded = isUploaded;
	private bool _hasCrcDefaults = hasCrcDefaults;

	/// <summary>The file.</summary>
	public OutputFileOption File { get; } = file;

	/// <summary>The file key, e.g. <c>Airways_High_Lines</c>.</summary>
	public string Key => File.Key;

	/// <summary>The checkbox label, e.g. <c>Lines</c>.</summary>
	public string Label => File.Label;

	/// <summary>Whether the file is GeoJSON, so can carry CRC-ERAM defaults.</summary>
	public bool IsGeojson => File.IsGeojson;

	/// <summary>Whether the file goes to vNAS.</summary>
	public bool IsUploaded
	{
		get => _isUploaded;
		set
		{
			if (SetProperty(ref _isUploaded, value))
			{
				_onChanged(this);
			}
		}
	}

	/// <summary>Whether the file gets CRC-ERAM defaults, when they go on specific files only.</summary>
	public bool HasCrcDefaults
	{
		get => _hasCrcDefaults;
		set
		{
			if (SetProperty(ref _hasCrcDefaults, value))
			{
				_onChanged(this);
			}
		}
	}
}

using FeBuddy.Wpf.Mvvm;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One GeoJSON file on the CRC ERAM Defaults card's "Specific files" list: whether it gets
/// CRC-ERAM defaults.
/// </summary>
/// <remarks>
/// The owning tab keeps the saved choices and rebuilds these whenever the files it writes
/// change, so a toggle is only ever a view of those choices; <paramref name="onChanged"/> hands
/// each change back to it.
/// </remarks>
/// <param name="file">The file.</param>
/// <param name="hasCrcDefaults">Whether it starts chosen for CRC-ERAM defaults.</param>
/// <param name="onChanged">Called with this toggle when the user ticks or unticks it.</param>
public sealed class CrcFileToggle(OutputFileOption file, bool hasCrcDefaults, Action<CrcFileToggle> onChanged) : ObservableObject
{
	private readonly Action<CrcFileToggle> _onChanged = onChanged;
	private bool _hasCrcDefaults = hasCrcDefaults;

	/// <summary>The file.</summary>
	public OutputFileOption File { get; } = file;

	/// <summary>The file key, e.g. <c>Airways_High_Lines</c>.</summary>
	public string Key => File.Key;

	/// <summary>The checkbox label, e.g. <c>Lines</c>.</summary>
	public string Label => File.Label;

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

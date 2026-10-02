using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;
using FeBuddy.Wpf.ViewModels.Models;

using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Backs <c>AliasGuideFormatWindow</c>, the question Export FE-Buddy Alias Command Guide asks first:
/// a web page, Markdown, or both. Web is chosen to start with. The folder is asked for next.
/// </summary>
public sealed class AliasGuideFormatViewModel : ObservableObject
{
	private AliasGuideFormatChoice _choice = AliasGuideFormatChoice.Web;

	/// <summary>Creates the view-model, with Web chosen.</summary>
	public AliasGuideFormatViewModel()
	{
		ContinueCommand = new RelayCommand(() => Close(confirmed: true));
		CancelCommand = new RelayCommand(() => Close(confirmed: false));
	}

	/// <summary>Raised when the window should close.</summary>
	public event EventHandler? CloseRequested;

	/// <summary>The format chosen.</summary>
	public AliasGuideFormatChoice Choice
	{
		get => _choice;
		set
		{
			if (SetProperty(ref _choice, value))
			{
				OnPropertyChanged(nameof(IsWeb));
				OnPropertyChanged(nameof(IsMarkdown));
				OnPropertyChanged(nameof(IsBoth));
			}
		}
	}

	/// <summary>A web page: the first radio button.</summary>
	public bool IsWeb
	{
		get => Choice == AliasGuideFormatChoice.Web;
		set { if (value) Choice = AliasGuideFormatChoice.Web; }
	}

	/// <summary>Markdown: the second radio button.</summary>
	public bool IsMarkdown
	{
		get => Choice == AliasGuideFormatChoice.Markdown;
		set { if (value) Choice = AliasGuideFormatChoice.Markdown; }
	}

	/// <summary>One of each: the third radio button.</summary>
	public bool IsBoth
	{
		get => Choice == AliasGuideFormatChoice.Both;
		set { if (value) Choice = AliasGuideFormatChoice.Both; }
	}

	/// <summary>The files to write for <see cref="Choice"/>, web page first.</summary>
	public IReadOnlyList<AliasGuideFormat> Formats => Choice switch
	{
		AliasGuideFormatChoice.Markdown => [AliasGuideFormat.Markdown],
		AliasGuideFormatChoice.Both => [AliasGuideFormat.Html, AliasGuideFormat.Markdown],
		_ => [AliasGuideFormat.Html],
	};

	/// <summary><see langword="true"/> once the user chose Choose folder…; <see langword="false"/> if they cancelled or closed the window.</summary>
	public bool Confirmed { get; private set; }

	/// <summary>Closes the window and goes on to the folder.</summary>
	public ICommand ContinueCommand { get; }

	/// <summary>Closes the window without exporting.</summary>
	public ICommand CancelCommand { get; }

	private void Close(bool confirmed)
	{
		Confirmed = confirmed;
		CloseRequested?.Invoke(this, EventArgs.Empty);
	}
}

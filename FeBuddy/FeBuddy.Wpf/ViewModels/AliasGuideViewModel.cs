using System.Windows.Input;

using FeBuddy.Wpf.Mvvm;

using FeBuddy.Core.Application.AliasGuide;
using FeBuddy.Core.Application.AliasGuide.Models;

namespace FeBuddy.Wpf.ViewModels;

/// <summary>
/// Info ▸ Alias Command Guide: the guide to every FE-Buddy alias command, shown in the app exactly as
/// it is exported (<see cref="AliasGuideContent"/>), with the button that exports it
/// (<see cref="AliasGuideExport"/>).
/// </summary>
/// <param name="back">Returns to where the page was opened from: Info's cards, or What's New.</param>
public sealed class AliasGuideViewModel(Action back) : ObservableObject
{
	/// <summary>The guide, naming the user's facility when Settings ▸ Facility Profile has one.</summary>
	public AliasGuideDocument Document { get; } = AliasGuideContent.Build(AliasGuideExport.Facility);

	/// <summary>Returns to where the page was opened from.</summary>
	public ICommand BackCommand { get; } = new RelayCommand(back);

	/// <summary>Asks what format to save the guide in, then which folder, then writes it.</summary>
	public ICommand ExportGuideCommand { get; } = new RelayCommand(AliasGuideExport.Run);
}

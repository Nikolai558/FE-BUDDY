using System.Windows.Input;

namespace FeBuddy.Wpf.ViewModels.Models;

/// <summary>
/// One of FE-Buddy's alias files on the vNAS Alias Upload tab: which sub-service writes it, and
/// whether the current settings put it into <c>vNAS_Alias.txt</c> - and if not, why not.
/// </summary>
/// <param name="SubService">The sub-service's name, e.g. <c>Airways</c>.</param>
/// <param name="FileName">Its alias file, e.g. <c>Airways.txt</c>.</param>
/// <param name="Status">What happens to it, e.g. <c>Not ticked on its Upload to vNAS card</c>.</param>
/// <param name="IsAdded">Whether it goes into <c>vNAS_Alias.txt</c>.</param>
/// <param name="OpenTabCommand">Opens the sub-service's tab, or <see langword="null"/> when it is not selected.</param>
public sealed record FeBuddyAliasFileRow(string SubService, string FileName, string Status, bool IsAdded, ICommand? OpenTabCommand)
{
	/// <summary>Whether the sub-service's tab can be opened from the row.</summary>
	public bool CanOpenTab => OpenTabCommand is not null;
}

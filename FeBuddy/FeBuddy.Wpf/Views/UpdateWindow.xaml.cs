using System.Windows;

using Microsoft.Win32;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.Views.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// The modal update window. Closes itself when its view-model raises
/// <see cref="UpdateWindowViewModel.CloseRequested"/>, owns the "close FE-Buddy with unfinished
/// work?" prompt and, going back, the question about settings the older release can't read, and
/// cancels a download in progress when it is closed.
/// </summary>
public partial class UpdateWindow : ChromeWindow
{
	/// <summary>Creates the window. Set its DataContext to an <see cref="UpdateWindowViewModel"/>.</summary>
	public UpdateWindow()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
		Closed += (_, _) => (DataContext as UpdateWindowViewModel)?.CancelDownload();
	}

	private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.OldValue is UpdateWindowViewModel oldVm)
		{
			oldVm.CloseRequested -= OnCloseRequested;
			oldVm.ConfirmCloseWithUnfinishedWork = null;
			oldVm.AskAboutUnreadableSettings = null;
			oldVm.PickExportFolder = null;
		}

		if (e.NewValue is UpdateWindowViewModel newVm)
		{
			newVm.CloseRequested += OnCloseRequested;
			newVm.ConfirmCloseWithUnfinishedWork = ConfirmCloseWithUnfinishedWork;
			newVm.AskAboutUnreadableSettings = profiles => AskAboutUnreadableSettings(newVm, profiles);
			newVm.PickExportFolder = PickExportFolder;
		}
	}

	private bool ConfirmCloseWithUnfinishedWork(IReadOnlyList<string> work) =>
		ConfirmWindow.Show(
			this,
			"Close FE-Buddy to update?",
			"FE-Buddy closes while the update installs, and this will be lost:\n\n"
				+ string.Join("\n", work.Select(item => "•  " + item)),
			confirmText: "Update anyway");

	private ConfirmChoice AskAboutUnreadableSettings(UpdateWindowViewModel vm, IReadOnlyList<string> profiles) =>
		ConfirmWindow.ShowChoice(
			this,
			vm.UnreadableSettingsHeading,
			vm.DescribeUnreadableSettings(profiles),
			confirmText: "Export…",
			alternativeText: "Continue without export",
			cancelText: "Cancel");

	private string? PickExportFolder()
	{
		OpenFolderDialog dialog = new()
		{
			Title = "Choose where to export your settings",
			InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
		};

		return dialog.ShowDialog(this) == true ? dialog.FolderName : null;
	}

	private void OnCloseRequested(object? sender, EventArgs e) => Close();
}

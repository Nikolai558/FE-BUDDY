using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;

using FeBuddy.Installer.UI.Setup;
using FeBuddy.Installer.UI.ViewModels;

namespace FeBuddy.Installer.UI.Views;

/// <summary>The setup window. Its behaviour is <see cref="SetupViewModel"/>'s; this wires the view-only parts.</summary>
public partial class SetupWindow : Window
{
	private readonly SetupViewModel _viewModel;
	private bool _closeApproved;

	/// <summary>Creates the window for <paramref name="viewModel"/>.</summary>
	/// <param name="viewModel">The run's view-model.</param>
	public SetupWindow(SetupViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		DataContext = viewModel;

		viewModel.ShowPrompt = request => PromptWindow.Ask(this, request);
		viewModel.PickFolder = start => FolderPicker.Pick(new WindowInteropHelper(this).Handle, "Choose where to install FE-BUDDY", start);
		viewModel.CloseRequested += CloseNow;
		ContentRendered += (_, _) => BringToFront();
	}

	/// <summary>Closes the window without asking (the run is over, or never started).</summary>
	public void CloseNow()
	{
		_closeApproved = true;
		Close();
	}

	/// <inheritdoc />
	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);
		if (!_closeApproved && !_viewModel.OnWindowClosing())
		{
			e.Cancel = true;
		}
	}

	/// <summary>
	/// Windows Installer runs this window in a process of its own, which Windows may open behind
	/// whatever was in front; topmost for a moment puts it on top without taking over the screen.
	/// </summary>
	private void BringToFront()
	{
		Topmost = true;
		Activate();
		Topmost = false;
	}

	private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

	private void OnClose(object sender, RoutedEventArgs e) => Close();
}

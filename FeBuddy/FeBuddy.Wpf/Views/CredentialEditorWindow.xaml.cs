using System.Windows;

using FeBuddy.Wpf.Controls;
using FeBuddy.Wpf.ViewModels;

using FeBuddy.Core.Infrastructure.Credentials;
using FeBuddy.Core.Infrastructure.Credentials.Models;

namespace FeBuddy.Wpf.Views;

/// <summary>
/// Adds or edits one credential (<see cref="CredentialEditorViewModel"/>). A PasswordBox cannot be
/// bound, which suits a secret: the window hands what is typed to the view-model, never the other
/// way, and clears both when it closes.
/// </summary>
public partial class CredentialEditorWindow : ChromeWindow
{
	private readonly CredentialEditorViewModel _viewModel;

	private CredentialEditorWindow(CredentialEditorViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		DataContext = viewModel;

		SecretBox.PasswordChanged += (_, _) => viewModel.Secret = SecretBox.Password;
		viewModel.CloseRequested += (_, _) => Close();
		Loaded += (_, _) => NameBox.Focus();
		Closed += (_, _) =>
		{
			SecretBox.Clear();
			viewModel.Secret = string.Empty;
		};
	}

	/// <summary>Shows the editor modally.</summary>
	/// <param name="owner">The window to centre on.</param>
	/// <param name="store">Where the credential is saved.</param>
	/// <param name="existing">The credential to edit, or <see langword="null"/> to add one.</param>
	/// <returns>The saved credential, or <see langword="null"/> when cancelled.</returns>
	public static CredentialInfo? Edit(Window? owner, CredentialStore store, CredentialInfo? existing)
	{
		CredentialEditorWindow window = new(new CredentialEditorViewModel(store, existing)) { Owner = owner };
		window.ShowDialog();
		return window._viewModel.Result;
	}
}

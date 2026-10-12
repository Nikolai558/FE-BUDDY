using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using FeBuddy.Installer.UI.ViewModels;

using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.UI.Views;

/// <summary>A themed prompt: shows a <see cref="PromptRequest"/> modally and returns the choice.</summary>
public partial class PromptWindow : Window
{
	private readonly PromptRequest _request;
	private MessageResult _result;

	private PromptWindow(PromptRequest request)
	{
		InitializeComponent();
		_request = request;
		_result = request.CloseResult;

		(string glyph, string brush, string soft) = request.Kind switch
		{
			PromptKind.Error => ("Icon.Error", "Brush.Danger", "Brush.Danger.Soft"),
			PromptKind.Warning => ("Icon.Warning", "Brush.Warn", "Brush.Warn.Soft"),
			_ => ("Icon.Info", "Brush.Accent", "Brush.Accent.Soft"),
		};
		Glyph.SetResourceReference(TextBlock.TextProperty, glyph);
		Glyph.SetResourceReference(TextBlock.ForegroundProperty, brush);
		Badge.SetResourceReference(Border.BackgroundProperty, soft);

		Heading.Text = request.Title;
		Message.Text = request.Message;
		Message.Visibility = string.IsNullOrWhiteSpace(request.Message) ? Visibility.Collapsed : Visibility.Visible;

		if (request.Items.Count > 0)
		{
			Items.ItemsSource = request.Items;
			ItemsPanel.Visibility = Visibility.Visible;
		}

		// The main action last, as the app's dialogs have it.
		foreach (PromptButton choice in request.Buttons.OrderBy(b => b.IsPrimary))
		{
			Button button = new()
			{
				Content = choice.Label,
				Margin = new Thickness(10, 0, 0, 0),
				IsDefault = choice.IsPrimary,
			};
			button.SetResourceReference(StyleProperty, choice.IsPrimary
				? choice.IsDanger ? "Button.Danger" : "Button.Primary"
				: choice.IsDanger ? "Button.Ghost.Danger" : "Button.Ghost");
			button.Click += (_, _) =>
			{
				_result = choice.Result;
				Close();
			};
			Buttons.Children.Add(button);
		}
	}

	/// <summary>Shows <paramref name="request"/> over <paramref name="owner"/> and waits for a choice.</summary>
	/// <param name="owner">The setup window, or <see langword="null"/>.</param>
	/// <param name="request">The prompt.</param>
	/// <returns>The chosen button's result, or the request's close result.</returns>
	public static MessageResult Ask(Window? owner, PromptRequest request)
	{
		PromptWindow prompt = new(request);
		if (owner is not null && owner.IsVisible)
		{
			prompt.Owner = owner;
		}
		else
		{
			prompt.WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}

		prompt.ShowDialog();
		return prompt._result;
	}

	/// <inheritdoc />
	protected override void OnClosing(CancelEventArgs e)
	{
		base.OnClosing(e);

		// Alt+F4 means "close": the request's close result, already the default.
		if (_result == MessageResult.None)
		{
			_result = _request.CloseResult;
		}
	}
}

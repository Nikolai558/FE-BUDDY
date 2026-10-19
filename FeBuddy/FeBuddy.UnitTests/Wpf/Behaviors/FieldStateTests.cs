using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;

using FeBuddy.Wpf.Behaviors;

namespace FeBuddy.UnitTests.Wpf.Behaviors;

/// <summary>
/// Covers <see cref="FieldState"/>: an error marks the box, and a box's hint is the tool-tip the theme's styles show
/// until an error replaces it - which a local ToolTip, always winning over a style, would never let happen.
/// </summary>
public sealed class FieldStateTests
{
	/// <summary>Setting an error marks the box; clearing it unmarks it.</summary>
	[Fact]
	public void an_error_marks_the_box_until_it_is_cleared() =>
		StaThread.Run(() =>
		{
			ComboBox box = new();

			FieldState.SetError(box, "Pick one of the listed values.");
			Assert.True(FieldState.GetHasError(box));
			Assert.Equal("Pick one of the listed values.", FieldState.GetError(box));

			FieldState.SetError(box, " ");
			Assert.False(FieldState.GetHasError(box));
		});

	/// <summary>
	/// With the theme's own styles - a text box's and a drop-down's - the hint shows until there is an error, then the
	/// error does. The boxes sit in XAML that declares the <c>bhv</c> prefix the styles' bindings name, as every view does.
	/// </summary>
	[Fact]
	public void the_hint_shows_until_an_error_replaces_it() =>
		StaThread.Run(() =>
		{
			ResourceDictionary inputs = (ResourceDictionary)System.Windows.Application.LoadComponent(
				new Uri("/FE-BUDDY;component/Theme/Controls.Inputs.xaml", UriKind.Relative));
			Panel view = (Panel)XamlReader.Parse(
				"<StackPanel xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" " +
				"xmlns:bhv=\"clr-namespace:FeBuddy.Wpf.Behaviors;assembly=FE-BUDDY\" />");

			foreach ((Control box, object style) in new (Control, object)[] { (new TextBox(), typeof(TextBox)), (new ComboBox(), "Themed.ComboBox") })
			{
				view.Children.Add(box);
				box.Style = (Style)inputs[style];
				FieldState.SetHint(box, "BCG group: an integer 1-40.");
				Pump();
				Assert.Equal("BCG group: an integer 1-40.", FieldState.GetHint(box));
				Assert.Equal("BCG group: an integer 1-40.", box.ToolTip);

				FieldState.SetError(box, "Pick one of the listed values.");
				Pump();
				Assert.Equal("Pick one of the listed values.", box.ToolTip);

				FieldState.SetError(box, null);
				Pump();
				Assert.Equal("BCG group: an integer 1-40.", box.ToolTip);
			}
		});

	/// <summary>
	/// No box in FE-Buddy's views both shows an error and sets its own tool-tip: a local one always wins over the
	/// theme's, so the error would never show. A box's hint goes in <see cref="FieldState.HintProperty"/> instead.
	/// </summary>
	[Fact]
	public void no_box_that_shows_an_error_sets_its_own_tool_tip()
	{
		string app = Path.Combine(RepositoryRoot(), "FeBuddy", "FeBuddy.Wpf");
		string[] skipped = [$"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"];
		List<string> both = [];

		foreach (string file in Directory.EnumerateFiles(app, "*.xaml", SearchOption.AllDirectories)
			.Where(file => !skipped.Any(folder => file.Contains(folder, StringComparison.OrdinalIgnoreCase))))
		{
			foreach (XElement element in XDocument.Load(file, LoadOptions.SetLineInfo).Descendants())
			{
				bool showsError = element.Attributes().Any(attribute => attribute.Name.LocalName == "FieldState.Error");
				bool ownToolTip = element.Attribute("ToolTip") is not null
					|| element.Elements().Any(child => child.Name.LocalName == element.Name.LocalName + ".ToolTip");

				if (showsError && ownToolTip)
				{
					both.Add($"{Path.GetRelativePath(app, file)}:{((IXmlLineInfo)element).LineNumber}");
				}
			}
		}

		Assert.Empty(both);
	}

	/// <summary>Lets the bindings the theme's styles set up run: WPF runs one only once the dispatcher gets to it.</summary>
	private static void Pump() =>
		System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

	/// <summary>The folder holding <c>FeBuddy\FeBuddy.sln</c>, found by walking up from the test's own folder.</summary>
	private static string RepositoryRoot()
	{
		for (DirectoryInfo? folder = new(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
		{
			if (File.Exists(Path.Combine(folder.FullName, "FeBuddy", "FeBuddy.sln")))
			{
				return folder.FullName;
			}
		}

		throw new DirectoryNotFoundException($"No repository root (holding FeBuddy\\FeBuddy.sln) above {AppContext.BaseDirectory}.");
	}
}

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using FeBuddy.Wpf.Behaviors;

namespace FeBuddy.UnitTests.Wpf.Behaviors;

/// <summary>
/// Covers <see cref="FieldState"/>: an error marks the box, and a box's hint (W5-3) is the tool-tip a style shows
/// until an error replaces it - which a local ToolTip, always winning over a style, never let happen.
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

	/// <summary>Styled as the theme styles a box, the hint shows until there is an error, then the error does.</summary>
	[Fact]
	public void the_hint_shows_until_an_error_replaces_it() =>
		StaThread.Run(() =>
		{
			Style style = new(typeof(ComboBox));
			style.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,
				new Binding { Path = new PropertyPath(FieldState.HintProperty), RelativeSource = RelativeSource.Self }));
			Trigger error = new() { Property = FieldState.HasErrorProperty, Value = true };
			error.Setters.Add(new Setter(FrameworkElement.ToolTipProperty,
				new Binding { Path = new PropertyPath(FieldState.ErrorProperty), RelativeSource = RelativeSource.Self }));
			style.Triggers.Add(error);

			ComboBox box = new() { Style = style };
			FieldState.SetHint(box, "BCG group: an integer 1-40.");
			Assert.Equal("BCG group: an integer 1-40.", FieldState.GetHint(box));
			Assert.Equal("BCG group: an integer 1-40.", box.ToolTip);

			FieldState.SetError(box, "Pick one of the listed values.");
			Assert.Equal("Pick one of the listed values.", box.ToolTip);

			FieldState.SetError(box, null);
			Assert.Equal("BCG group: an integer 1-40.", box.ToolTip);
		});
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FeBuddy.Installer.UI.Controls;

/// <summary>
/// The FEB logo, as FeBuddy.Wpf's BrandMark draws it: the smallest pixel-exact export at least as
/// large as the mark on screen, picked again when the window changes monitor. The exports are
/// FeBuddy.Wpf's Assets/Brand/FEB_*.png, linked into this assembly.
/// </summary>
/// <remarks>
/// The images are named with this assembly in their pack URI: inside msiexec.exe there is no
/// application assembly for a bare <c>pack://application:,,,/Assets/...</c> to resolve against.
/// </remarks>
public sealed class BrandMark : Image
{
	private static readonly int[] Sizes = [24, 32, 40, 48, 64, 96];

	private static readonly Dictionary<int, BitmapImage> Cache = [];

	/// <summary>Creates the mark and picks its image once it knows its monitor.</summary>
	public BrandMark()
	{
		RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
		Loaded += (_, _) => PickSource(VisualTreeHelper.GetDpi(this).PixelsPerDip);
	}

	/// <inheritdoc />
	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		base.OnDpiChanged(oldDpi, newDpi);
		PickSource(newDpi.PixelsPerDip);
	}

	private void PickSource(double pixelsPerDip)
	{
		double wanted = (double.IsNaN(Width) ? ActualWidth : Width) * pixelsPerDip;
		int size = Sizes.FirstOrDefault(s => s >= wanted - 0.5);
		Source = Load(size == 0 ? Sizes[^1] : size);
	}

	private static BitmapImage Load(int size)
	{
		if (!Cache.TryGetValue(size, out BitmapImage? image))
		{
			image = new BitmapImage(new Uri($"pack://application:,,,/FeBuddy.Installer.UI;component/Assets/Brand/FEB_{size}.png"));
			image.Freeze();
			Cache[size] = image;
		}

		return image;
	}
}

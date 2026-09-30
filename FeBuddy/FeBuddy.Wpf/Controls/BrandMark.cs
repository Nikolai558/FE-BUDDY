using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// The FEB logo, drawn from the pixel-exact export for the monitor's scaling instead of one
/// image squeezed to fit (which blurs it). Picks the smallest export at least as large as the
/// mark's size in device pixels, and picks again when the window moves to another monitor.
/// Size it with Width/Height; 24 is pixel-exact at 100% scaling.
/// <code>
/// &lt;ctl:BrandMark Width="24" Height="24" /&gt;
/// </code>
/// </summary>
public sealed class BrandMark : Image
{
	// Pixel sizes shipped under Assets/Brand (FEB_{size}.png), smallest first.
	private static readonly int[] Sizes = [24, 32, 40, 48, 64, 96];

	private static readonly Dictionary<int, BitmapImage> Cache = [];

	/// <summary>Creates the mark and picks its first image once it knows its monitor.</summary>
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
		int size = Sizes.FirstOrDefault(s => s >= wanted - 0.5, Sizes[^1]);
		Source = Load(size);
	}

	private static BitmapImage Load(int size)
	{
		if (!Cache.TryGetValue(size, out var image))
		{
			image = new BitmapImage(new Uri($"pack://application:,,,/Assets/Brand/FEB_{size}.png"));
			image.Freeze();
			Cache[size] = image;
		}
		return image;
	}
}

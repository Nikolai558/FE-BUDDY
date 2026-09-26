using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using FeBuddy.Wpf.Map;
using FeBuddy.Wpf.Map.Models;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A dependency-free interactive vector map.
/// <para>
/// Purely presentation: Web-Mercator projection, a pan/zoom viewport, and
/// <see cref="StreamGeometry"/> rendering into a few <see cref="DrawingVisual"/>s. No tiles,
/// no network, no map SDK.
/// </para>
/// <para>
/// The world repeats side by side, like a strip of wallpaper: panning never hits an edge, and
/// everything - layers, the grid, the ROI box - is drawn once for each copy of the world in
/// view. Latitude stops at the Mercator limit, so the map cannot be dragged off the poles.
/// </para>
/// <para>
/// Mouse: left-drag pans (right- and middle-drag always pan too), the wheel zooms about the
/// pointer, a double-click zooms in. <b>Shift + left-drag</b> draws an ROI box at any time and
/// raises <see cref="RoiQuickDrawn"/> on release. With <see cref="RoiEditing"/> on, a left-drag
/// draws a new box, drags the box's handles to resize it, or drags inside it to move it.
/// Keys: arrows pan, + and - zoom.
/// </para>
/// <para>
/// Dense data is kept readable: a layer below its <see cref="MapLayer.MinZoom"/>, or with more
/// points or labels in view than the map can usefully draw, is held back and named in
/// <see cref="DensityHint"/> until the user zooms in.
/// </para>
/// </summary>
public sealed class MapCanvas : FrameworkElement
{
	/// <summary>The deepest zoom level (about half a metre per pixel).</summary>
	private const double MaxZoom = 18.0;

	/// <summary>More dots than this in view and a layer's points wait for a closer zoom.</summary>
	private const int PointBudget = 8_000;

	/// <summary>More labels than this in view and a layer's labels wait for a closer zoom.</summary>
	private const int LabelBudget = 700;

	/// <summary>A drag shorter than this (pixels) is a click, not a box.</summary>
	private const double ClickSlop = 4.0;

	/// <summary>How close (pixels) the pointer has to be to grab an ROI handle or edge.</summary>
	private const double HandleGrab = 7.0;

	private const double HandleSize = 8.0;

	// --- viewport ------------------------------------------------------------
	private double _scale = 1_000;   // pixels per world unit
	private double _centerX = 0.5;   // world-unit point at screen centre; x kept in 0..1
	private double _centerY = 0.5;
	private bool _framed;

	// --- interaction ---------------------------------------------------------
	private DragMode _drag;
	private MouseButton _dragButton;
	private Point _dragStartScreen;
	private Point _lastScreen;
	private bool _dragMoved;
	private bool _dragIsQuick;
	private Edges _resizeEdges;
	private WorldRect _draft;       // the box being drawn/moved/resized, in world units

	// --- rendering -----------------------------------------------------------
	private readonly VisualCollection _visuals;
	private readonly DrawingVisual _worldVisual = new();
	private readonly DrawingVisual _roiVisual = new();
	private readonly DrawingVisual _overlayVisual = new();
	private bool _renderQueued;
	private readonly Dictionary<(string Text, Brush Brush), FormattedText> _textCache = [];
	private Typeface? _labelFace;

	/// <summary>Creates an empty map; it frames the contiguous US on its first layout.</summary>
	public MapCanvas()
	{
		_visuals = new VisualCollection(this) { _worldVisual, _roiVisual, _overlayVisual };
		ClipToBounds = true;
		Focusable = true;
		FocusVisualStyle = null;
		SnapsToDevicePixels = true;
		Cursor = Cursors.Arrow;
	}

	/// <summary>Raised when a Shift + drag finishes drawing a box; <see cref="Roi"/> already holds it.</summary>
	public event EventHandler<GeoBounds>? RoiQuickDrawn;

	private enum DragMode
	{
		None,
		Pan,
		DrawRoi,
		MoveRoi,
		ResizeRoi,
	}

	[Flags]
	private enum Edges
	{
		None = 0,
		Left = 1,
		Right = 2,
		Top = 4,
		Bottom = 8,
	}

	// ======================= dependency properties =========================

	/// <summary>Identifies the <see cref="BaseLayer"/> dependency property.</summary>
	public static readonly DependencyProperty BaseLayerProperty = DependencyProperty.Register(
		nameof(BaseLayer), typeof(MapLayer), typeof(MapCanvas),
		new PropertyMetadata(null, OnMapDataChanged));

	/// <summary>Identifies the <see cref="Layers"/> dependency property.</summary>
	public static readonly DependencyProperty LayersProperty = DependencyProperty.Register(
		nameof(Layers), typeof(IEnumerable<MapLayer>), typeof(MapCanvas),
		new PropertyMetadata(null, OnLayersChanged));

	/// <summary>Identifies the <see cref="Roi"/> dependency property.</summary>
	public static readonly DependencyProperty RoiProperty = DependencyProperty.Register(
		nameof(Roi), typeof(GeoBounds?), typeof(MapCanvas),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRoiChanged));

	/// <summary>Identifies the <see cref="ReferenceRoi"/> dependency property.</summary>
	public static readonly DependencyProperty ReferenceRoiProperty = DependencyProperty.Register(
		nameof(ReferenceRoi), typeof(GeoBounds?), typeof(MapCanvas),
		new PropertyMetadata(null, OnRoiChanged));

	/// <summary>Identifies the <see cref="RoiEditing"/> dependency property.</summary>
	public static readonly DependencyProperty RoiEditingProperty = DependencyProperty.Register(
		nameof(RoiEditing), typeof(bool), typeof(MapCanvas),
		new PropertyMetadata(false, OnRoiChanged));

	/// <summary>Identifies the <see cref="ShowGraticule"/> dependency property.</summary>
	public static readonly DependencyProperty ShowGraticuleProperty = DependencyProperty.Register(
		nameof(ShowGraticule), typeof(bool), typeof(MapCanvas),
		new PropertyMetadata(true, OnMapDataChanged));

	private static readonly DependencyPropertyKey CursorTextPropertyKey = DependencyProperty.RegisterReadOnly(
		nameof(CursorText), typeof(string), typeof(MapCanvas), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="CursorText"/> dependency property.</summary>
	public static readonly DependencyProperty CursorTextProperty = CursorTextPropertyKey.DependencyProperty;

	private static readonly DependencyPropertyKey DensityHintPropertyKey = DependencyProperty.RegisterReadOnly(
		nameof(DensityHint), typeof(string), typeof(MapCanvas), new PropertyMetadata(string.Empty));

	/// <summary>Identifies the <see cref="DensityHint"/> dependency property.</summary>
	public static readonly DependencyProperty DensityHintProperty = DensityHintPropertyKey.DependencyProperty;

	/// <summary>The always-on background layer (state outlines), drawn under everything.</summary>
	public MapLayer? BaseLayer
	{
		get => (MapLayer?)GetValue(BaseLayerProperty);
		set => SetValue(BaseLayerProperty, value);
	}

	/// <summary>Overlay layers drawn on top of the base, in order.</summary>
	public IEnumerable<MapLayer>? Layers
	{
		get => (IEnumerable<MapLayer>?)GetValue(LayersProperty);
		set => SetValue(LayersProperty, value);
	}

	/// <summary>
	/// The ROI box, or <see langword="null"/> for none. Two-way: drawing, moving or resizing the
	/// box writes it. Its longitudes are folded so the box's centre is within -180..180; a box
	/// drawn across the 180th meridian therefore has an edge past ±180, which the host rejects.
	/// </summary>
	public GeoBounds? Roi
	{
		get => (GeoBounds?)GetValue(RoiProperty);
		set => SetValue(RoiProperty, value);
	}

	/// <summary>A second box drawn dashed for comparison (the default ROI while editing an override).</summary>
	public GeoBounds? ReferenceRoi
	{
		get => (GeoBounds?)GetValue(ReferenceRoiProperty);
		set => SetValue(ReferenceRoiProperty, value);
	}

	/// <summary>When on, left-drag edits the ROI (draw, move, resize) and the rest of the map dims.</summary>
	public bool RoiEditing
	{
		get => (bool)GetValue(RoiEditingProperty);
		set => SetValue(RoiEditingProperty, value);
	}

	/// <summary>Whether the latitude/longitude grid is drawn. On by default.</summary>
	public bool ShowGraticule
	{
		get => (bool)GetValue(ShowGraticuleProperty);
		set => SetValue(ShowGraticuleProperty, value);
	}

	/// <summary>Lat/lon under the pointer, e.g. <c>38.51203, -95.10412</c>. Empty when off-map.</summary>
	public string CursorText => (string)GetValue(CursorTextProperty);

	/// <summary>
	/// The layers held back until the user zooms in, e.g. <c>Zoom in to see Fixes_Symbols</c>,
	/// or empty when everything is drawn.
	/// </summary>
	public string DensityHint => (string)GetValue(DensityHintProperty);

	private double Zoom => WebMercator.ScaleToZoom(_scale);

	private double MinScale => Math.Max(WebMercator.TileSize, ActualHeight * 0.9);

	private static double MaxScale => WebMercator.ZoomToScale(MaxZoom);

	// ============================ public API ===============================

	/// <summary>Frames the contiguous US.</summary>
	public void ResetView()
		=> FrameBounds(new GeoBounds(new GeoPoint(24.0, -125.0), new GeoPoint(50.0, -66.0)));

	/// <summary>Zooms and pans so <paramref name="bounds"/> fills the view with a margin.</summary>
	/// <param name="bounds">The area to show.</param>
	public void FrameBounds(GeoBounds bounds) => FrameWorld(
		WebMercator.LonToWorldX(bounds.West), WebMercator.LonToWorldX(bounds.East),
		WebMercator.LatToWorldY(bounds.North), WebMercator.LatToWorldY(bounds.South));

	/// <summary>
	/// Zooms and pans to show every shape in <paramref name="layers"/>. A layer over the 180th
	/// meridian is framed the short way round.
	/// </summary>
	/// <param name="layers">The layers to show.</param>
	/// <returns><see langword="false"/> when the layers hold nothing to frame.</returns>
	public bool FrameLayers(IEnumerable<MapLayer> layers)
	{
		double x0 = double.MaxValue, x1 = double.MinValue, y0 = double.MaxValue, y1 = double.MinValue;
		foreach (MapLayer layer in layers)
		{
			ProjectedLayer projected = ProjectedLayer.For(layer);
			if (projected.IsEmpty)
			{
				continue;
			}

			x0 = Math.Min(x0, projected.MinX);
			x1 = Math.Max(x1, projected.MaxX);
			y0 = Math.Min(y0, projected.MinY);
			y1 = Math.Max(y1, projected.MaxY);
		}

		if (x0 > x1)
		{
			return false;
		}

		FrameWorld(x0, x1, y0, y1);
		return true;
	}

	/// <summary>Zooms about the centre of the view.</summary>
	/// <param name="factor">Above 1 zooms in, below 1 zooms out.</param>
	public void ZoomBy(double factor) => ZoomAbout(new Point(ActualWidth / 2.0, ActualHeight / 2.0), factor);

	/// <summary>Where the map is looking, to hand to <see cref="SetView"/> later.</summary>
	/// <returns>The current view, or <see langword="null"/> before the map has laid out.</returns>
	public MapViewState? GetView() => _framed ? new MapViewState(_centerX, _centerY, _scale) : null;

	/// <summary>Restores a view saved by <see cref="GetView"/>.</summary>
	/// <param name="view">The view.</param>
	public void SetView(MapViewState view)
	{
		_centerX = view.CenterX;
		_centerY = view.CenterY;
		_scale = view.Scale;
		_framed = true;
		ClampView();
		InvalidateMap();
	}

	private void FrameWorld(double x0, double x1, double y0, double y1)
	{
		if (ActualWidth < 1 || ActualHeight < 1)
		{
			return;
		}

		double worldW = Math.Max(x1 - x0, 1e-7);
		double worldH = Math.Max(y1 - y0, 1e-7);

		_centerX = (x0 + x1) / 2.0;
		_centerY = (y0 + y1) / 2.0;
		_scale = Math.Min(Math.Min(ActualWidth / worldW, ActualHeight / worldH) * 0.88, WebMercator.ZoomToScale(12));
		_framed = true;
		ClampView();
		InvalidateMap();
	}

	// ========================== visual plumbing ============================

	/// <inheritdoc />
	protected override int VisualChildrenCount => _visuals.Count;

	/// <inheritdoc />
	protected override Visual GetVisualChild(int index) => _visuals[index];

	/// <inheritdoc />
	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		if (!_framed)
		{
			ResetView();
		}
		else
		{
			ClampView();
			InvalidateMap();
		}
	}

	/// <inheritdoc />
	protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
	{
		base.OnDpiChanged(oldDpi, newDpi);
		_textCache.Clear();
		InvalidateMap();
	}

	// ============================ projection ===============================

	private double ScreenX(double worldX) => ((worldX - _centerX) * _scale) + (ActualWidth / 2.0);

	private double ScreenY(double worldY) => ((worldY - _centerY) * _scale) + (ActualHeight / 2.0);

	private double WorldX(double screenX) => ((screenX - (ActualWidth / 2.0)) / _scale) + _centerX;

	private double WorldY(double screenY) => ((screenY - (ActualHeight / 2.0)) / _scale) + _centerY;

	/// <summary>Keeps x in 0..1 (the wrap) and y inside the world (no dragging off the poles).</summary>
	private void ClampView()
	{
		_scale = Math.Clamp(_scale, MinScale, MaxScale);
		_centerX -= Math.Floor(_centerX);

		double halfH = ActualHeight / 2.0 / _scale;
		_centerY = halfH >= 0.5 ? 0.5 : Math.Clamp(_centerY, halfH, 1.0 - halfH);
	}

	/// <summary>The world-unit rectangle in view, grown by <paramref name="marginPx"/> on every side.</summary>
	private WorldRect ViewWorld(double marginPx) => new(
		WorldX(-marginPx), WorldX(ActualWidth + marginPx),
		WorldY(-marginPx), WorldY(ActualHeight + marginPx));

	/// <summary>The whole-world offsets at which something spanning x0..x1 shows in <paramref name="view"/>.</summary>
	private static (int First, int Last) Copies(double x0, double x1, WorldRect view) =>
		((int)Math.Ceiling(view.X0 - x1), (int)Math.Floor(view.X1 - x0));

	// ============================= input ==================================

	/// <inheritdoc />
	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		base.OnMouseDown(e);
		if (_drag != DragMode.None)
		{
			return;   // one gesture at a time
		}

		Focus();
		Point pos = e.GetPosition(this);
		_dragStartScreen = _lastScreen = pos;
		_dragMoved = false;
		_dragButton = e.ChangedButton;

		if (e.ChangedButton == MouseButton.Left)
		{
			if (e.ClickCount == 2 && !RoiEditing && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
			{
				ZoomAbout(pos, 2.0);
				e.Handled = true;
				return;
			}

			BeginLeftDrag(pos);
		}
		else if (e.ChangedButton is MouseButton.Right or MouseButton.Middle)
		{
			_drag = DragMode.Pan;
		}
		else
		{
			return;
		}

		CaptureMouse();
		UpdateCursor(pos);
		e.Handled = true;
	}

	private void BeginLeftDrag(Point pos)
	{
		_dragIsQuick = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
		if (_dragIsQuick)
		{
			_drag = DragMode.DrawRoi;
			_draft = new WorldRect(WorldX(pos.X), WorldX(pos.X), WorldY(pos.Y), WorldY(pos.Y));
			return;
		}

		if (!RoiEditing)
		{
			_drag = DragMode.Pan;
			return;
		}

		if (HitTestRoi(pos, out WorldRect box, out Edges edges))
		{
			_draft = box;
			_resizeEdges = edges;
			_drag = edges == Edges.None ? DragMode.MoveRoi : DragMode.ResizeRoi;
			return;
		}

		_drag = DragMode.DrawRoi;
		_draft = new WorldRect(WorldX(pos.X), WorldX(pos.X), WorldY(pos.Y), WorldY(pos.Y));
	}

	/// <inheritdoc />
	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Point pos = e.GetPosition(this);

		SetValue(CursorTextPropertyKey, Format(WebMercator.WorldYToLat(WorldY(pos.Y)), WebMercator.NormalizeLon(WebMercator.WorldXToLon(WorldX(pos.X)))));

		if (_drag == DragMode.None)
		{
			UpdateCursor(pos);
			return;
		}

		if (!_dragMoved && (Math.Abs(pos.X - _dragStartScreen.X) > ClickSlop || Math.Abs(pos.Y - _dragStartScreen.Y) > ClickSlop))
		{
			_dragMoved = true;
		}

		double dx = (pos.X - _lastScreen.X) / _scale;
		double dy = (pos.Y - _lastScreen.Y) / _scale;
		_lastScreen = pos;

		switch (_drag)
		{
			case DragMode.Pan:
				_centerX -= dx;
				_centerY -= dy;
				ClampView();
				InvalidateMap();
				break;

			case DragMode.DrawRoi:
				_draft = new WorldRect(WorldX(_dragStartScreen.X), WorldX(pos.X), WorldY(_dragStartScreen.Y), WorldY(pos.Y)).Normalized();
				RedrawRoi();
				break;

			case DragMode.MoveRoi:
				double moveY = Math.Clamp(dy, -_draft.Y0, 1.0 - _draft.Y1);
				_draft = new WorldRect(_draft.X0 + dx, _draft.X1 + dx, _draft.Y0 + moveY, _draft.Y1 + moveY);
				RedrawRoi();
				break;

			case DragMode.ResizeRoi:
				ResizeDraft(pos);
				RedrawRoi();
				break;
		}
	}

	private void ResizeDraft(Point pos)
	{
		double x = WorldX(pos.X);
		double y = Math.Clamp(WorldY(pos.Y), 0.0, 1.0);
		WorldRect d = _draft;

		// An edge dragged past its opposite simply stops there - flipping the box mid-drag is
		// never what the user meant.
		const double minSize = 1e-7;
		if (_resizeEdges.HasFlag(Edges.Left))
		{
			d = d with { X0 = Math.Min(x, d.X1 - minSize) };
		}

		if (_resizeEdges.HasFlag(Edges.Right))
		{
			d = d with { X1 = Math.Max(x, d.X0 + minSize) };
		}

		if (_resizeEdges.HasFlag(Edges.Top))
		{
			d = d with { Y0 = Math.Min(y, d.Y1 - minSize) };
		}

		if (_resizeEdges.HasFlag(Edges.Bottom))
		{
			d = d with { Y1 = Math.Max(y, d.Y0 + minSize) };
		}

		_draft = d;
	}

	/// <inheritdoc />
	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		base.OnMouseUp(e);
		if (_drag == DragMode.None || e.ChangedButton != _dragButton)
		{
			return;
		}

		DragMode finished = _drag;
		bool quick = _dragIsQuick;
		_drag = DragMode.None;
		ReleaseMouseCapture();

		if (finished is DragMode.DrawRoi or DragMode.MoveRoi or DragMode.ResizeRoi && _dragMoved)
		{
			GeoBounds box = ToGeo(_draft);
			SetCurrentValue(RoiProperty, box);
			if (finished == DragMode.DrawRoi && quick)
			{
				RoiQuickDrawn?.Invoke(this, box);
			}
		}

		RedrawRoi();
		UpdateCursor(e.GetPosition(this));
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnLostMouseCapture(MouseEventArgs e)
	{
		base.OnLostMouseCapture(e);
		if (_drag != DragMode.None)
		{
			_drag = DragMode.None;   // e.g. a dialog stole the mouse: drop the gesture
			RedrawRoi();
		}
	}

	/// <inheritdoc />
	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		ZoomAbout(e.GetPosition(this), Math.Pow(1.25, e.Delta / 120.0));
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		double step = 120.0 / _scale;
		switch (e.Key)
		{
			case Key.Left: _centerX -= step; break;
			case Key.Right: _centerX += step; break;
			case Key.Up: _centerY -= step; break;
			case Key.Down: _centerY += step; break;
			case Key.OemPlus or Key.Add: ZoomBy(1.5); e.Handled = true; return;
			case Key.OemMinus or Key.Subtract: ZoomBy(1.0 / 1.5); e.Handled = true; return;
			default: return;
		}

		ClampView();
		InvalidateMap();
		e.Handled = true;
	}

	/// <inheritdoc />
	protected override void OnMouseLeave(MouseEventArgs e)
	{
		base.OnMouseLeave(e);
		SetValue(CursorTextPropertyKey, string.Empty);
	}

	private void ZoomAbout(Point screen, double factor)
	{
		// Re-centre so the point under the cursor stays put.
		double wx = WorldX(screen.X);
		double wy = WorldY(screen.Y);
		_scale = Math.Clamp(_scale * factor, MinScale, MaxScale);
		_centerX = wx - ((screen.X - (ActualWidth / 2.0)) / _scale);
		_centerY = wy - ((screen.Y - (ActualHeight / 2.0)) / _scale);
		ClampView();
		InvalidateMap();
	}

	private void UpdateCursor(Point pos)
	{
		Cursor = _drag switch
		{
			DragMode.Pan => Cursors.ScrollAll,
			DragMode.DrawRoi => Cursors.Cross,
			DragMode.MoveRoi => Cursors.SizeAll,
			DragMode.ResizeRoi => EdgeCursor(_resizeEdges),
			_ when (Keyboard.Modifiers & ModifierKeys.Shift) != 0 => Cursors.Cross,
			_ when RoiEditing && HitTestRoi(pos, out _, out Edges edges) => edges == Edges.None ? Cursors.SizeAll : EdgeCursor(edges),
			_ when RoiEditing => Cursors.Cross,
			_ => Cursors.Arrow,
		};
	}

	private static Cursor EdgeCursor(Edges edges) => edges switch
	{
		Edges.Left | Edges.Top or Edges.Right | Edges.Bottom => Cursors.SizeNWSE,
		Edges.Right | Edges.Top or Edges.Left | Edges.Bottom => Cursors.SizeNESW,
		Edges.Left or Edges.Right => Cursors.SizeWE,
		_ => Cursors.SizeNS,
	};

	/// <summary>
	/// Finds the ROI copy under <paramref name="pos"/>: <paramref name="edges"/> names the edges
	/// being grabbed (a corner grabs two), or is <see cref="Edges.None"/> for the inside.
	/// </summary>
	private bool HitTestRoi(Point pos, out WorldRect box, out Edges edges)
	{
		box = default;
		edges = Edges.None;
		if (Roi is not { } roi)
		{
			return false;
		}

		WorldRect world = ToWorld(roi);
		(int first, int last) = Copies(world.X0, world.X1, ViewWorld(HandleGrab));
		for (int k = first; k <= last; k++)
		{
			WorldRect copy = world.Shifted(k);
			double left = ScreenX(copy.X0), right = ScreenX(copy.X1), top = ScreenY(copy.Y0), bottom = ScreenY(copy.Y1);

			if (pos.X < left - HandleGrab || pos.X > right + HandleGrab || pos.Y < top - HandleGrab || pos.Y > bottom + HandleGrab)
			{
				continue;
			}

			if (Math.Abs(pos.X - left) <= HandleGrab) edges |= Edges.Left;
			else if (Math.Abs(pos.X - right) <= HandleGrab) edges |= Edges.Right;
			if (Math.Abs(pos.Y - top) <= HandleGrab) edges |= Edges.Top;
			else if (Math.Abs(pos.Y - bottom) <= HandleGrab) edges |= Edges.Bottom;

			bool inside = pos.X > left && pos.X < right && pos.Y > top && pos.Y < bottom;
			if (edges != Edges.None || inside)
			{
				box = copy;
				return true;
			}
		}

		return false;
	}

	// ======================== world <-> geo boxes ==========================

	private static WorldRect ToWorld(GeoBounds b) => new(
		WebMercator.LonToWorldX(b.West), WebMercator.LonToWorldX(b.East),
		WebMercator.LatToWorldY(b.North), WebMercator.LatToWorldY(b.South));

	/// <summary>
	/// A world box back to lat/lon, shifted by whole worlds so its centre is within -180..180
	/// and rounded to 4 decimal places (about 10 m - far finer than a pixel at any useful zoom).
	/// </summary>
	private static GeoBounds ToGeo(WorldRect w)
	{
		double shift = Math.Floor((w.X0 + w.X1) / 2.0);
		double west = WebMercator.WorldXToLon(w.X0 - shift);
		double east = WebMercator.WorldXToLon(w.X1 - shift);
		double north = WebMercator.WorldYToLat(Math.Clamp(w.Y0, 0.0, 1.0));
		double south = WebMercator.WorldYToLat(Math.Clamp(w.Y1, 0.0, 1.0));
		return new GeoBounds(
			new GeoPoint(Math.Round(south, 4), Math.Round(west, 4)),
			new GeoPoint(Math.Round(north, 4), Math.Round(east, 4)));
	}

	// ============================ rendering ================================

	private Brush Theme(string key, Color fallback)
		=> TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

	/// <summary>Queues one full redraw for the next frame, however many changes ask for it.</summary>
	private void InvalidateMap()
	{
		if (_renderQueued)
		{
			return;
		}

		_renderQueued = true;
		Dispatcher.BeginInvoke(DispatcherPriority.Render, Redraw);
	}

	private void Redraw()
	{
		_renderQueued = false;
		if (ActualWidth < 1 || ActualHeight < 1)
		{
			return;
		}

		List<string> heldBack = [];
		LabelPlacer labels = new();

		using (DrawingContext dc = _worldVisual.RenderOpen())
		{
			dc.DrawRectangle(Theme("Brush.Bg.Sunken", Color.FromRgb(0x07, 0x0B, 0x10)), null, new Rect(0, 0, ActualWidth, ActualHeight));

			GraticuleLines? grid = ShowGraticule ? DrawGraticule(dc) : null;

			if (BaseLayer is { } baseLayer)
			{
				DrawLayer(dc, baseLayer, labels, heldBack);
			}

			if (Layers is { } layers)
			{
				foreach (MapLayer layer in layers)
				{
					DrawLayer(dc, layer, labels, heldBack);
				}
			}

			if (grid is { } g)
			{
				DrawGraticuleLabels(dc, g);
			}
		}

		string hint = heldBack.Count == 0 ? string.Empty : "Zoom in to see " + string.Join(", ", heldBack.Distinct());
		if (!string.Equals(hint, DensityHint, StringComparison.Ordinal))
		{
			SetValue(DensityHintPropertyKey, hint);
		}

		RedrawRoi();
		DrawScaleBar();
	}

	// ---- layers ----

	private void DrawLayer(DrawingContext dc, MapLayer layer, LabelPlacer labels, List<string> heldBack)
	{
		ProjectedLayer projected = ProjectedLayer.For(layer);
		if (projected.IsEmpty)
		{
			return;
		}

		if (Zoom < layer.MinZoom)
		{
			heldBack.Add(layer.Name);
			return;
		}

		WorldRect view = ViewWorld(layer.Thickness + 2);
		(int first, int last) = Copies(projected.MinX, projected.MaxX, view);
		if (first > last || projected.MaxY < view.Y0 || projected.MinY > view.Y1)
		{
			return;
		}

		if (projected.Runs.Count > 0)
		{
			DrawRuns(dc, layer, projected, view, first, last);
		}

		if (projected.Points.Count > 0)
		{
			DrawPoints(dc, layer, projected, view, first, last, labels, heldBack);
		}
	}

	private void DrawRuns(DrawingContext dc, MapLayer layer, ProjectedLayer projected, WorldRect view, int first, int last)
	{
		Pen pen = new(layer.Stroke, layer.Thickness) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
		pen.Freeze();

		// Clip in screen space against the view plus a margin, so a line running far off-screen
		// never hands WPF coordinates in the millions (which it renders badly, and slowly).
		Rect clip = new(-16, -16, ActualWidth + 32, ActualHeight + 32);
		StreamGeometry geometry = new();
		using (StreamGeometryContext ctx = geometry.Open())
		{
			RunWriter writer = new(ctx, clip);
			for (int k = first; k <= last; k++)
			{
				foreach (ProjectedRun run in projected.Runs)
				{
					if (run.MaxX + k < view.X0 || run.MinX + k > view.X1 || run.MaxY < view.Y0 || run.MinY > view.Y1)
					{
						continue;
					}

					writer.Begin();
					Point previous = new(ScreenX(run.Xs[0] + k), ScreenY(run.Ys[0]));
					int count = run.Xs.Length;
					for (int i = 1; i < count; i++)
					{
						Point next = new(ScreenX(run.Xs[i] + k), ScreenY(run.Ys[i]));
						writer.Segment(previous, next, isLast: !run.Closed && i == count - 1);
						previous = next;
					}

					if (run.Closed)
					{
						writer.Segment(previous, new Point(ScreenX(run.Xs[0] + k), ScreenY(run.Ys[0])), isLast: true);
					}

					writer.End();
				}
			}
		}

		geometry.Freeze();
		dc.DrawGeometry(null, pen, geometry);
	}

	private void DrawPoints(
		DrawingContext dc, MapLayer layer, ProjectedLayer projected, WorldRect view, int first, int last,
		LabelPlacer labels, List<string> heldBack)
	{
		List<Point> dots = [];
		List<(Point At, string Text)> texts = [];
		bool dotsOver = false, textsOver = false;
		bool wantLabels = Zoom >= layer.LabelMinZoom;

		for (int k = first; k <= last; k++)
		{
			foreach (ProjectedPoint point in projected.Points)
			{
				double x = point.X + k;
				if (x < view.X0 || x > view.X1 || point.Y < view.Y0 || point.Y > view.Y1)
				{
					continue;
				}

				Point at = new(ScreenX(x), ScreenY(point.Y));
				if (point.Label is null)
				{
					if (!dotsOver)
					{
						dots.Add(at);
						dotsOver = dots.Count > PointBudget;
					}
				}
				else if (wantLabels && !textsOver)
				{
					texts.Add((at, point.Label));
					textsOver = texts.Count > LabelBudget;
				}
			}
		}

		if (dotsOver || textsOver)
		{
			heldBack.Add(layer.Name);
		}

		if (!dotsOver && dots.Count > 0)
		{
			double r = layer.PointRadius;
			StreamGeometry geometry = new() { FillRule = FillRule.Nonzero };   // even-odd would punch holes where dots overlap
			using (StreamGeometryContext ctx = geometry.Open())
			{
				Size radius = new(r, r);
				foreach (Point p in dots)
				{
					ctx.BeginFigure(new Point(p.X - r, p.Y), isFilled: true, isClosed: true);
					ctx.ArcTo(new Point(p.X + r, p.Y), radius, 0, false, SweepDirection.Clockwise, true, false);
					ctx.ArcTo(new Point(p.X - r, p.Y), radius, 0, false, SweepDirection.Clockwise, true, false);
				}
			}

			geometry.Freeze();
			dc.DrawGeometry(layer.Stroke, null, geometry);
		}

		if (!textsOver)
		{
			foreach ((Point at, string text) in texts)
			{
				FormattedText formatted = Text(text, layer.Stroke);
				Rect box = new(at.X - (formatted.Width / 2.0), at.Y - (formatted.Height / 2.0), formatted.Width, formatted.Height);
				if (labels.TryPlace(box))
				{
					dc.DrawText(formatted, box.TopLeft);
				}
			}
		}
	}

	private FormattedText Text(string text, Brush brush)
	{
		if (_textCache.TryGetValue((text, brush), out FormattedText? cached))
		{
			return cached;
		}

		if (_textCache.Count > 6_000)
		{
			_textCache.Clear();
		}

		_labelFace ??= new Typeface(
			TryFindResource("Font.Mono") as FontFamily ?? new FontFamily("Consolas"),
			FontStyles.Normal, FontWeights.Medium, FontStretches.Normal);

		FormattedText formatted = new(
			text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _labelFace, 11, brush,
			VisualTreeHelper.GetDpi(this).PixelsPerDip)
		{
			TextAlignment = TextAlignment.Left,
		};
		_textCache[(text, brush)] = formatted;
		return formatted;
	}

	// ---- graticule ----

	private readonly record struct GraticuleLines(double Step, List<(double Lon, double X)> Meridians, List<(double Lat, double Y)> Parallels);

	private GraticuleLines DrawGraticule(DrawingContext dc)
	{
		SolidColorBrush lineBrush = new(Color.FromArgb(0x12, 0xFF, 0xFF, 0xFF));
		lineBrush.Freeze();
		Pen pen = new(lineBrush, 1);
		pen.Freeze();
		SolidColorBrush strongBrush = new(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));
		strongBrush.Freeze();
		Pen strong = new(strongBrush, 1);
		strong.Freeze();

		// Aim for a line roughly every 100 pixels.
		double pixelsPerDegree = _scale / 360.0;
		double[] steps = [0.1, 0.25, 0.5, 1, 2, 5, 10, 15, 30];
		double step = steps.FirstOrDefault(s => s * pixelsPerDegree >= 100, 30);

		double top = Math.Max(ScreenY(0), 0);
		double bottom = Math.Min(ScreenY(1), ActualHeight);
		List<(double, double)> meridians = [];
		List<(double, double)> parallels = [];

		// Count in whole steps rather than adding step to itself, so 0.1-degree lines don't drift.
		long lonFirst = (long)Math.Floor(WebMercator.WorldXToLon(WorldX(0)) / step);
		long lonLast = (long)Math.Ceiling(WebMercator.WorldXToLon(WorldX(ActualWidth)) / step);
		for (long i = lonFirst; i <= lonLast; i++)
		{
			double lon = i * step;
			double x = Math.Round(ScreenX(WebMercator.LonToWorldX(lon))) + 0.5;
			bool seam = Math.Abs(WebMercator.NormalizeLon(lon + 180.0)) < 1e-9;
			dc.DrawLine(seam ? strong : pen, new Point(x, top), new Point(x, bottom));
			meridians.Add((lon, x));
		}

		double latTop = Math.Min(WebMercator.WorldYToLat(WorldY(0)), WebMercator.MaxLatitude);
		double latBottom = Math.Max(WebMercator.WorldYToLat(WorldY(ActualHeight)), -WebMercator.MaxLatitude);
		for (long i = (long)Math.Ceiling(latBottom / step); i * step <= latTop; i++)
		{
			double lat = i * step;
			double y = Math.Round(ScreenY(WebMercator.LatToWorldY(lat))) + 0.5;
			dc.DrawLine(Math.Abs(lat) < 1e-9 ? strong : pen, new Point(0, y), new Point(ActualWidth, y));
			parallels.Add((lat, y));
		}

		return new GraticuleLines(step, meridians, parallels);
	}

	private void DrawGraticuleLabels(DrawingContext dc, GraticuleLines grid)
	{
		Brush brush = Theme("Brush.Text.Tertiary", Color.FromRgb(0x8B, 0x9D, 0xAD));
		string format = grid.Step < 1 ? "0.##" : "0";

		foreach ((double lon, double x) in grid.Meridians)
		{
			double folded = WebMercator.NormalizeLon(lon);
			string text = Math.Abs(folded) < 1e-9 ? "0°"
				: Math.Abs(folded + 180.0) < 1e-9 ? "180°"
				: Math.Abs(folded).ToString(format, CultureInfo.InvariantCulture) + (folded < 0 ? "°W" : "°E");
			// Along the bottom edge: the top-left corner belongs to the host's toolbar.
			FormattedText formatted = Text(text, brush);
			if (x + 4 + formatted.Width < ActualWidth)
			{
				dc.DrawText(formatted, new Point(x + 4, ActualHeight - formatted.Height - 5));
			}
		}

		foreach ((double lat, double y) in grid.Parallels)
		{
			string text = Math.Abs(lat) < 1e-9 ? "0°"
				: Math.Abs(lat).ToString(format, CultureInfo.InvariantCulture) + (lat < 0 ? "°S" : "°N");
			FormattedText formatted = Text(text, brush);

			// Clear of the toolbar above and the scale bar below.
			double top = y - formatted.Height - 2;
			if (top > 64 && top < ActualHeight - 72)
			{
				dc.DrawText(formatted, new Point(6, y - formatted.Height - 2));
			}
		}
	}

	// ---- scale bar ----

	private void DrawScaleBar()
	{
		using DrawingContext dc = _overlayVisual.RenderOpen();
		if (ActualWidth < 200 || ActualHeight < 120)
		{
			return;
		}

		double lat = WebMercator.WorldYToLat(_centerY);
		double nmPerPixel = WebMercator.EquatorNauticalMiles * Math.Cos(lat * Math.PI / 180.0) / _scale;
		double[] nice = [0.1, 0.2, 0.5, 1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 5000];
		double nm = nice.MinBy(n => Math.Abs((n / nmPerPixel) - 110));
		double length = nm / nmPerPixel;

		Brush text = Theme("Brush.Text.Secondary", Color.FromRgb(0xB7, 0xC6, 0xD3));
		Pen pen = new(text, 1.5) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square };
		pen.Freeze();

		double x0 = 14, y = ActualHeight - 30;   // above the longitude labels along the bottom
		dc.DrawLine(pen, new Point(x0, y), new Point(x0 + length, y));
		dc.DrawLine(pen, new Point(x0, y - 5), new Point(x0, y));
		dc.DrawLine(pen, new Point(x0 + length, y - 5), new Point(x0 + length, y));

		FormattedText label = Text(nm.ToString("0.#", CultureInfo.InvariantCulture) + " NM", text);
		dc.DrawText(label, new Point(x0, y - 6 - label.Height));
	}

	// ---- ROI ----

	private void RedrawRoi()
	{
		using DrawingContext dc = _roiVisual.RenderOpen();
		if (ActualWidth < 1 || ActualHeight < 1)
		{
			return;
		}

		bool dragging = _drag is DragMode.DrawRoi or DragMode.MoveRoi or DragMode.ResizeRoi;
		bool editing = RoiEditing || dragging;
		WorldRect view = ViewWorld(HandleSize);

		WorldRect? box = dragging ? _draft : Roi is { } roi ? ToWorld(roi) : null;

		List<Rect> copies = [];
		if (box is { } b)
		{
			(int first, int last) = Copies(b.X0, b.X1, view);
			for (int k = first; k <= last; k++)
			{
				WorldRect c = b.Shifted(k);
				copies.Add(new Rect(new Point(ScreenX(c.X0), ScreenY(c.Y0)), new Point(ScreenX(c.X1), ScreenY(c.Y1))));
			}
		}

		// While editing, dim everything outside the box so the box is what the eye lands on.
		if (editing)
		{
			GeometryGroup dim = new() { FillRule = FillRule.EvenOdd };
			dim.Children.Add(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
			foreach (Rect c in copies)
			{
				dim.Children.Add(new RectangleGeometry(Rect.Intersect(c, new Rect(-1, -1, ActualWidth + 2, ActualHeight + 2))));
			}

			dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(0x70, 0x03, 0x06, 0x0A)), null, dim);
		}

		if (ReferenceRoi is { } reference)
		{
			WorldRect r = ToWorld(reference);
			Pen dashed = new(Theme("Brush.Text.Tertiary", Color.FromRgb(0x8B, 0x9D, 0xAD)), 1.2) { DashStyle = DashStyles.Dash };
			(int first, int last) = Copies(r.X0, r.X1, view);
			for (int k = first; k <= last; k++)
			{
				WorldRect c = r.Shifted(k);
				dc.DrawRectangle(null, dashed, new Rect(new Point(ScreenX(c.X0), ScreenY(c.Y0)), new Point(ScreenX(c.X1), ScreenY(c.Y1))));
			}
		}

		if (copies.Count == 0)
		{
			return;
		}

		Brush accent = Theme("Brush.Accent", Color.FromRgb(0xF4, 0xB7, 0x40));
		SolidColorBrush fill = new(Color.FromArgb(editing ? (byte)0x14 : (byte)0x22, 0xF4, 0xB7, 0x40));
		Pen outline = new(accent, editing ? 2.0 : 1.5);
		Brush handleFill = Theme("Brush.Bg.Base", Color.FromRgb(0x0B, 0x0F, 0x14));
		Pen handlePen = new(accent, 1.5);

		foreach (Rect c in copies)
		{
			dc.DrawRectangle(fill, outline, c);
			if (!editing)
			{
				continue;
			}

			double midX = (c.Left + c.Right) / 2.0, midY = (c.Top + c.Bottom) / 2.0;
			foreach (Point h in new[]
			{
				c.TopLeft, c.TopRight, c.BottomLeft, c.BottomRight,
				new Point(midX, c.Top), new Point(midX, c.Bottom), new Point(c.Left, midY), new Point(c.Right, midY),
			})
			{
				dc.DrawRectangle(handleFill, handlePen, new Rect(h.X - (HandleSize / 2), h.Y - (HandleSize / 2), HandleSize, HandleSize));
			}
		}
	}

	private static string Format(double lat, double lon)
		=> $"{lat.ToString("0.00000", CultureInfo.InvariantCulture)}, {lon.ToString("0.00000", CultureInfo.InvariantCulture)}";

	// ======================= change notifications =========================

	private static void OnMapDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> ((MapCanvas)d).InvalidateMap();

	private static void OnRoiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		MapCanvas map = (MapCanvas)d;
		map.RedrawRoi();
		if (e.Property == RoiEditingProperty && map.IsMouseOver)
		{
			map.UpdateCursor(Mouse.GetPosition(map));
		}
	}

	private static void OnLayersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var map = (MapCanvas)d;

		if (e.OldValue is INotifyCollectionChanged oldObservable)
		{
			oldObservable.CollectionChanged -= map.OnLayersCollectionChanged;
		}

		if (e.NewValue is INotifyCollectionChanged newObservable)
		{
			newObservable.CollectionChanged += map.OnLayersCollectionChanged;
		}

		map.InvalidateMap();
	}

	private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateMap();

	// ============================== helpers ================================

	/// <summary>An axis-aligned box in world units; x may run past 0..1 (see the class remarks).</summary>
	private readonly record struct WorldRect(double X0, double X1, double Y0, double Y1)
	{
		public WorldRect Shifted(int worlds) => this with { X0 = X0 + worlds, X1 = X1 + worlds };

		public WorldRect Normalized() => new(Math.Min(X0, X1), Math.Max(X0, X1), Math.Clamp(Math.Min(Y0, Y1), 0, 1), Math.Clamp(Math.Max(Y0, Y1), 0, 1));
	}

	/// <summary>
	/// Writes a line's segments into a <see cref="StreamGeometryContext"/>, clipping each to a
	/// rectangle (Liang-Barsky) and dropping vertices less than a pixel from the last one drawn.
	/// A segment that leaves the rectangle ends the figure; the next one to come back starts a
	/// new figure where it re-enters.
	/// </summary>
	private sealed class RunWriter(StreamGeometryContext ctx, Rect clip)
	{
		private const double Tolerance = 0.75;

		private bool _open;
		private Point _last;
		private Point _pending;
		private bool _hasPending;

		public void Begin()
		{
			_open = false;
			_hasPending = false;
		}

		public void End() => Flush();

		public void Segment(Point a, Point b, bool isLast)
		{
			bool aIn = clip.Contains(a), bIn = clip.Contains(b);
			if (aIn && bIn)
			{
				if (!_open)
				{
					Start(a);
				}

				if (!isLast && Math.Abs(b.X - _last.X) < Tolerance && Math.Abs(b.Y - _last.Y) < Tolerance)
				{
					_pending = b;
					_hasPending = true;
					return;
				}

				_hasPending = false;
				Line(b);
				return;
			}

			Flush();
			if (!Clip(ref a, ref b))
			{
				_open = false;
				return;
			}

			if (!_open || !aIn)
			{
				Start(a);
			}

			Line(b);
			if (!bIn)
			{
				_open = false;
			}
		}

		private void Start(Point p)
		{
			ctx.BeginFigure(p, isFilled: false, isClosed: false);
			_open = true;
			_last = p;
		}

		private void Line(Point p)
		{
			ctx.LineTo(p, isStroked: true, isSmoothJoin: false);
			_last = p;
		}

		private void Flush()
		{
			if (_hasPending && _open)
			{
				Line(_pending);
			}

			_hasPending = false;
		}

		private bool Clip(ref Point a, ref Point b)
		{
			double dx = b.X - a.X, dy = b.Y - a.Y;
			double t0 = 0, t1 = 1;

			if (!Edge(-dx, a.X - clip.Left, ref t0, ref t1)
				|| !Edge(dx, clip.Right - a.X, ref t0, ref t1)
				|| !Edge(-dy, a.Y - clip.Top, ref t0, ref t1)
				|| !Edge(dy, clip.Bottom - a.Y, ref t0, ref t1))
			{
				return false;
			}

			Point start = new(a.X + (t0 * dx), a.Y + (t0 * dy));
			Point end = new(a.X + (t1 * dx), a.Y + (t1 * dy));
			a = start;
			b = end;
			return true;
		}

		private static bool Edge(double p, double q, ref double t0, ref double t1)
		{
			if (p == 0)
			{
				return q >= 0;
			}

			double r = q / p;
			if (p < 0)
			{
				if (r > t1) return false;
				if (r > t0) t0 = r;
			}
			else
			{
				if (r < t0) return false;
				if (r < t1) t1 = r;
			}

			return true;
		}
	}

	/// <summary>Keeps labels from piling on top of each other: first come, first placed.</summary>
	private sealed class LabelPlacer
	{
		private const double Cell = 96;
		private readonly Dictionary<(int, int), List<Rect>> _cells = [];

		public bool TryPlace(Rect box)
		{
			box.Inflate(2, 1);
			int x0 = (int)Math.Floor(box.Left / Cell), x1 = (int)Math.Floor(box.Right / Cell);
			int y0 = (int)Math.Floor(box.Top / Cell), y1 = (int)Math.Floor(box.Bottom / Cell);

			for (int x = x0; x <= x1; x++)
			{
				for (int y = y0; y <= y1; y++)
				{
					if (_cells.TryGetValue((x, y), out List<Rect>? placed) && placed.Any(p => p.IntersectsWith(box)))
					{
						return false;
					}
				}
			}

			for (int x = x0; x <= x1; x++)
			{
				for (int y = y0; y <= y1; y++)
				{
					if (!_cells.TryGetValue((x, y), out List<Rect>? placed))
					{
						_cells[(x, y)] = placed = [];
					}

					placed.Add(box);
				}
			}

			return true;
		}
	}
}

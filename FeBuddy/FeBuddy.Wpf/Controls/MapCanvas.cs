using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FeBuddy.Wpf.Map;

namespace FeBuddy.Wpf.Controls;

/// <summary>
/// A dependency-free interactive vector map.
///
/// <para>
/// It is <b>purely presentation</b>: Web-Mercator projection, a pan/zoom
/// viewport, and <see cref="System.Windows.Media.StreamGeometry"/> rendering into
/// a couple of <see cref="DrawingVisual"/>s. No tiles, no network, no map SDK.
/// Feed it a base layer (e.g. state outlines) and any number of overlay
/// <see cref="MapLayer"/>s; optionally let the user rubber-band a region of
/// interest whose corners come back through <see cref="RoiSouthWest"/> /
/// <see cref="RoiNorthEast"/>.
/// </para>
/// </summary>
public sealed class MapCanvas : FrameworkElement
{
    // --- viewport ------------------------------------------------------------
    private double _scale = 1_000;          // pixels per world unit
    private Point _center = new(0.5, 0.5);  // world-unit point at screen centre
    private bool _framed;

    // --- interaction state -------------------------------------------------
    private bool _panning;
    private Point _panLastScreen;
    private bool _roiDragging;
    private Point _roiStartScreen;
    private Point _roiCurrentScreen;

    private readonly VisualCollection _visuals;
    private readonly DrawingVisual _worldVisual = new();
    private readonly DrawingVisual _roiVisual = new();

    public MapCanvas()
    {
        _visuals = new VisualCollection(this) { _worldVisual, _roiVisual };
        ClipToBounds = true;
        Focusable = true;
        SnapsToDevicePixels = true;
    }

    // ======================= dependency properties =========================

    public static readonly DependencyProperty BaseLayerProperty = DependencyProperty.Register(
        nameof(BaseLayer), typeof(MapLayer), typeof(MapCanvas),
        new PropertyMetadata(null, OnMapDataChanged));

    public static readonly DependencyProperty LayersProperty = DependencyProperty.Register(
        nameof(Layers), typeof(IEnumerable<MapLayer>), typeof(MapCanvas),
        new PropertyMetadata(null, OnLayersChanged));

    public static readonly DependencyProperty RoiEnabledProperty = DependencyProperty.Register(
        nameof(RoiEnabled), typeof(bool), typeof(MapCanvas),
        new PropertyMetadata(false, OnRoiEnabledChanged));

    public static readonly DependencyProperty RoiSouthWestProperty = DependencyProperty.Register(
        nameof(RoiSouthWest), typeof(GeoPoint?), typeof(MapCanvas),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRoiChanged));

    public static readonly DependencyProperty RoiNorthEastProperty = DependencyProperty.Register(
        nameof(RoiNorthEast), typeof(GeoPoint?), typeof(MapCanvas),
        new FrameworkPropertyMetadata(null,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRoiChanged));

    public static readonly DependencyProperty ShowGraticuleProperty = DependencyProperty.Register(
        nameof(ShowGraticule), typeof(bool), typeof(MapCanvas),
        new PropertyMetadata(true, OnMapDataChanged));

    /// <summary>The always-on background layer (state / country outlines).</summary>
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

    /// <summary>When true, left-drag draws an ROI box instead of panning.</summary>
    public bool RoiEnabled
    {
        get => (bool)GetValue(RoiEnabledProperty);
        set => SetValue(RoiEnabledProperty, value);
    }

    public GeoPoint? RoiSouthWest
    {
        get => (GeoPoint?)GetValue(RoiSouthWestProperty);
        set => SetValue(RoiSouthWestProperty, value);
    }

    public GeoPoint? RoiNorthEast
    {
        get => (GeoPoint?)GetValue(RoiNorthEastProperty);
        set => SetValue(RoiNorthEastProperty, value);
    }

    public bool ShowGraticule
    {
        get => (bool)GetValue(ShowGraticuleProperty);
        set => SetValue(ShowGraticuleProperty, value);
    }

    // ============================ public API ===============================

    /// <summary>Frames the contiguous US.</summary>
    public void ResetView()
        => FrameBounds(new GeoBounds(new GeoPoint(24.0, -125.0), new GeoPoint(50.0, -66.0)));

    /// <summary>Zooms/pans so <paramref name="bounds"/> fills the view with a margin.</summary>
    public void FrameBounds(GeoBounds bounds)
    {
        if (ActualWidth < 1 || ActualHeight < 1)
        {
            return;
        }

        var x0 = WebMercator.LonToWorldX(bounds.West);
        var x1 = WebMercator.LonToWorldX(bounds.East);
        var y0 = WebMercator.LatToWorldY(bounds.North); // north -> smaller y
        var y1 = WebMercator.LatToWorldY(bounds.South);

        var worldW = Math.Max(Math.Abs(x1 - x0), 1e-6);
        var worldH = Math.Max(Math.Abs(y1 - y0), 1e-6);

        _center = new Point((x0 + x1) / 2.0, (y0 + y1) / 2.0);
        _scale = Math.Min(ActualWidth / worldW, ActualHeight / worldH) * 0.9;
        _framed = true;
        Redraw();
    }

    // ========================== visual plumbing ============================

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index) => _visuals[index];

    protected override void OnRenderSizeChanged(SizeChangedInfo info)
    {
        base.OnRenderSizeChanged(info);
        if (!_framed)
        {
            ResetView();
        }
        else
        {
            Redraw();
        }
    }

    // ============================ projection ===============================

    private Point ToScreen(GeoPoint p)
    {
        var wx = WebMercator.LonToWorldX(p.Lon);
        var wy = WebMercator.LatToWorldY(p.Lat);
        return new Point(
            ((wx - _center.X) * _scale) + (ActualWidth / 2.0),
            ((wy - _center.Y) * _scale) + (ActualHeight / 2.0));
    }

    private GeoPoint ToGeo(Point screen)
    {
        var wx = ((screen.X - (ActualWidth / 2.0)) / _scale) + _center.X;
        var wy = ((screen.Y - (ActualHeight / 2.0)) / _scale) + _center.Y;
        return new GeoPoint(WebMercator.WorldYToLat(wy), WebMercator.WorldXToLon(wx));
    }

    // ============================= input ==================================

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        CaptureMouse();

        if (RoiEnabled)
        {
            _roiDragging = true;
            _roiStartScreen = _roiCurrentScreen = e.GetPosition(this);
            RedrawRoi();
        }
        else
        {
            _panning = true;
            _panLastScreen = e.GetPosition(this);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);

        if (_panning)
        {
            var dx = (pos.X - _panLastScreen.X) / _scale;
            var dy = (pos.Y - _panLastScreen.Y) / _scale;
            _center = new Point(_center.X - dx, _center.Y - dy);
            _panLastScreen = pos;
            Redraw();
        }
        else if (_roiDragging)
        {
            _roiCurrentScreen = pos;
            RedrawRoi();
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        ReleaseMouseCapture();

        if (_roiDragging)
        {
            _roiDragging = false;
            var a = ToGeo(_roiStartScreen);
            var b = ToGeo(_roiCurrentScreen);
            if (Math.Abs(_roiStartScreen.X - _roiCurrentScreen.X) < 3 &&
                Math.Abs(_roiStartScreen.Y - _roiCurrentScreen.Y) < 3)
            {
                return; // treat a click as "no box"
            }

            SetCurrentValue(RoiSouthWestProperty,
                (GeoPoint?)new GeoPoint(Math.Min(a.Lat, b.Lat), Math.Min(a.Lon, b.Lon)));
            SetCurrentValue(RoiNorthEastProperty,
                (GeoPoint?)new GeoPoint(Math.Max(a.Lat, b.Lat), Math.Max(a.Lon, b.Lon)));
        }

        _panning = false;
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);

        // Right-click clears the ROI box.
        SetCurrentValue(RoiSouthWestProperty, null);
        SetCurrentValue(RoiNorthEastProperty, null);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        var factor = e.Delta > 0 ? 1.2 : 1.0 / 1.2;
        var newScale = Math.Clamp(_scale * factor, 200.0, 40_000_000.0);
        factor = newScale / _scale;

        // Keep the world point under the cursor fixed.
        var cursor = e.GetPosition(this);
        var before = ToGeo(cursor);
        _scale = newScale;
        var after = ToGeo(cursor);
        _center = new Point(
            _center.X + (WebMercator.LonToWorldX(before.Lon) - WebMercator.LonToWorldX(after.Lon)),
            _center.Y + (WebMercator.LatToWorldY(before.Lat) - WebMercator.LatToWorldY(after.Lat)));

        Redraw();
    }

    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        Cursor = RoiEnabled ? Cursors.Cross : Cursors.SizeAll;
    }

    // ============================ rendering ================================

    private Brush Theme(string key, Color fallback)
        => TryFindResource(key) as Brush ?? new SolidColorBrush(fallback);

    private void Redraw()
    {
        if (ActualWidth < 1 || ActualHeight < 1)
        {
            return;
        }

        var bg = Theme("Brush.Bg.Sunken", Color.FromRgb(0x07, 0x0B, 0x10));
        var graticule = new SolidColorBrush(Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF));
        graticule.Freeze();

        using (var dc = _worldVisual.RenderOpen())
        {
            dc.DrawRectangle(bg, null, new Rect(0, 0, ActualWidth, ActualHeight));

            if (ShowGraticule)
            {
                DrawGraticule(dc, new Pen(graticule, 1));
            }

            if (BaseLayer is { } baseLayer)
            {
                DrawLayer(dc, baseLayer);
            }

            if (Layers is { } layers)
            {
                foreach (var layer in layers)
                {
                    DrawLayer(dc, layer);
                }
            }
        }

        RedrawRoi();
    }

    private void DrawGraticule(DrawingContext dc, Pen pen)
    {
        for (var lon = -180; lon <= 180; lon += 10)
        {
            var a = ToScreen(new GeoPoint(WebMercator.MaxLatitude, lon));
            var b = ToScreen(new GeoPoint(-WebMercator.MaxLatitude, lon));
            dc.DrawLine(pen, a, b);
        }

        for (var lat = -80; lat <= 80; lat += 10)
        {
            var a = ToScreen(new GeoPoint(lat, -180));
            var b = ToScreen(new GeoPoint(lat, 180));
            dc.DrawLine(pen, a, b);
        }
    }

    private void DrawLayer(DrawingContext dc, MapLayer layer)
    {
        var pen = new Pen(layer.Stroke, layer.Thickness) { LineJoin = PenLineJoin.Round };
        pen.Freeze();

        foreach (var geometry in layer.Geometries)
        {
            if (geometry.Kind == MapGeometryKind.Point)
            {
                foreach (var run in geometry.Parts)
                {
                    if (run.Count > 0)
                    {
                        dc.DrawEllipse(layer.Stroke, null, ToScreen(run[0]), layer.PointRadius, layer.PointRadius);
                    }
                }

                continue;
            }

            var closed = geometry.Kind == MapGeometryKind.Polygon;
            var stream = new StreamGeometry();
            using (var ctx = stream.Open())
            {
                foreach (var run in geometry.Parts)
                {
                    if (run.Count < 2)
                    {
                        continue;
                    }

                    ctx.BeginFigure(ToScreen(run[0]), isFilled: false, isClosed: closed);
                    for (var i = 1; i < run.Count; i++)
                    {
                        ctx.LineTo(ToScreen(run[i]), isStroked: true, isSmoothJoin: false);
                    }
                }
            }

            stream.Freeze();
            dc.DrawGeometry(null, pen, stream);
        }
    }

    private void RedrawRoi()
    {
        using var dc = _roiVisual.RenderOpen();

        Rect rect;
        if (_roiDragging)
        {
            rect = new Rect(_roiStartScreen, _roiCurrentScreen);
        }
        else if (RoiSouthWest is { } sw && RoiNorthEast is { } ne)
        {
            var p1 = ToScreen(new GeoPoint(ne.Lat, sw.Lon));
            var p2 = ToScreen(new GeoPoint(sw.Lat, ne.Lon));
            rect = new Rect(p1, p2);
        }
        else
        {
            return;
        }

        var stroke = Theme("Brush.Accent", Color.FromRgb(0xF4, 0xB7, 0x40));
        var fill = new SolidColorBrush(Color.FromArgb(0x22, 0xF4, 0xB7, 0x40));
        fill.Freeze();
        dc.DrawRectangle(fill, new Pen(stroke, 1.5), rect);
    }

    // ======================= change notifications =========================

    private static void OnMapDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MapCanvas)d).Redraw();

    private static void OnRoiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MapCanvas)d).RedrawRoi();

    private static void OnRoiEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var map = (MapCanvas)d;
        map.Cursor = map.RoiEnabled ? Cursors.Cross : Cursors.SizeAll;
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

        map.Redraw();
    }

    private void OnLayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();
}

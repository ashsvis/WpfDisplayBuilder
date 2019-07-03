using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WpfDisplayBuilder
{
    public delegate void ZoomInEventHandler(object sender, EventArgs e);

    public delegate void ZoomOutEventHandler(object sender, EventArgs e);

    public delegate void SelectUpdatedEventHandler(object sender, SelectUpdatedEventArgs e);

    public class SelectUpdatedEventArgs : EventArgs
    {
        public List<Fig> List { get; set; }
        public Fig Item { get; set; }
        public bool HasGroups { get; set; }
        public bool MoreOneSelected { get; set; }
        public bool MoreTwoSelected { get; set; }
        public bool HasSelected { get; set; }
    }

    public enum ZoomMode
    {
        [Description("400%")]
        Scale400,
        [Description("200%")]
        Scale200,
        [Description("150%")]
        Scale150,
        [Description("125%")]
        Scale125,
        [Description("100%")]
        Scale100,
        [Description("80%")]
        Scale80,
        [Description("60%")]
        Scale60,
        [Description("50%")]
        Scale50,
        [Description("30%")]
        Scale30,
        [Description("Авто")]
        ZoomToFit
    }

    public enum EditMode
    {
        SelectMove,
        ZoomIn,
        ZoomOut,
        AddRectangle,
        AddCircle,
        AddHyperlink,
        AddLabel,
        AddTextbox,
        AddCheckbox,
        AddButton,
        AddCombobox,
        AddImage,
        AddShape,
        Delete,
        SelectMultiple
    }

    public enum EditorMode
    {
        AsScheme,
        AsShape,
        AsRuntime
    }

    /// <summary>
    /// Логика взаимодействия для DrawingPage.xaml
    /// </summary>
    public partial class DrawingPage : UserControl
    {
        public event ZoomInEventHandler OnZoomIn;

        public event ZoomOutEventHandler OnZoomOut;

        public event EventHandler ContentChanged;

        public event SelectUpdatedEventHandler SelectUpdated;

        private EditMode _editMode = EditMode.SelectMove;

        public EditorMode EditorMode { get; private set; }

        public Scheme Scheme { get; private set; }
        public Shape Shape { get; private set; }

        public EditMode EditMode
        {
            get { return _editMode; }
            set
            {
                if (value != EditMode.SelectMove)
                    ClearSelection();
                _editMode = value;
            }
        }

        public string HomeFolder { get; set; }

        private string _shapefolder;

        private string _imagefolder;

        private bool _runtimeMode;

        private readonly Timer _timer = new Timer(500) {AutoReset = true};

        public DrawingPage()
        {
            InitializeComponent();
            Focusable = true;
            EditMode = EditMode.SelectMove;
            EditorMode = EditorMode.AsScheme;
            Scheme = new Scheme {Width = _schemeWidth, Height = _schemeHeight};
            SetBindingSize(Scheme);
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;
        }

        private bool _blink;

        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _blink = !_blink;
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                    {
                        foreach (var fig in _figlist.Keys.Cast<DrawingVisual>()
                                                    .Select(visual => (Fig)_figlist[visual]))
                        {
                            fig.Drawme(_blink);
                        }
                    }));
        }

        private void SetBindingSize(object source)
        {
            SetBinding(PageWidthProperty,
                       new Binding { Source = source, Path = new PropertyPath("Width"), Mode = BindingMode.TwoWay });
            SetBinding(PageHeightProperty,
                       new Binding { Source = source, Path = new PropertyPath("Height"), Mode = BindingMode.TwoWay });
            //if (!(source is Scheme)) return;
            SetBinding(PageFillColorProperty,
                       new Binding
                           {
                               Source = source,
                               Path = new PropertyPath("FillColor"),
                               Mode = BindingMode.TwoWay
                           });
            SetBinding(PageFillImageProperty,
                       new Binding
                           {
                               Source = source,
                               Path = new PropertyPath("BackgroundImageUri"),
                               Mode = BindingMode.TwoWay
                           });
        }

        public static readonly DependencyProperty PageWidthProperty = DependencyProperty.Register(
            "PageWidth",
            typeof(double),
            typeof(DrawingPage),
            new PropertyMetadata(PropertyChanged));

        private double _schemeWidth = 1920.0;
        private double PageWidth { get { return _schemeWidth; } set { _schemeWidth = value; } }

        public static readonly DependencyProperty PageHeightProperty = DependencyProperty.Register(
            "PageHeight",
            typeof(double),
            typeof(DrawingPage),
            new PropertyMetadata(PropertyChanged));

        private double _schemeHeight = 1080.0;
        private double PageHeight { get { return _schemeHeight; } set { _schemeHeight = value; } }

        private Color PageFillColor { get; set; }

        public static readonly DependencyProperty PageFillColorProperty = DependencyProperty.Register(
            "PageFillColor",
            typeof(Color),
            typeof(DrawingPage),
            new PropertyMetadata(PropertyChanged));

        public static readonly DependencyProperty PageFillImageProperty = DependencyProperty.Register(
            "PageFillImage",
            typeof(string),
            typeof(DrawingPage),
            new PropertyMetadata(PropertyChanged));

        private static void PropertyChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs e)
        {
            var control = depObj as DrawingPage;
            if (control == null || e.NewValue == null) return;
            if (e.Property == PageFillColorProperty)
            {
                control.Scrollview.Background = new SolidColorBrush((Color)e.NewValue);
                if (!(control.Surface.Background is ImageBrush))
                    control.Surface.Background = new SolidColorBrush((Color) e.NewValue);
            }
            else if (e.Property == PageFillImageProperty)
            {
                var folder = control.GetFilesFolder();
                var filename = e.NewValue.ToString();
                var imagefile = Path.Combine(folder, filename);
                if (File.Exists(imagefile))
                {
                    var imageData = File.ReadAllBytes(imagefile);
                    var image = new BitmapImage();
                    using (var mem = new MemoryStream(imageData))
                    {
                        mem.Position = 0;
                        image.BeginInit();
                        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.UriSource = null;
                        image.StreamSource = mem;
                        image.EndInit();
                    }
                    image.Freeze();
                    control.Surface.Background = new ImageBrush(image) {Stretch = Stretch.None};
                }
                else if (control.Scheme != null)
                    control.Surface.Background = new SolidColorBrush(control.Scheme.FillColor); 
            }
            else if (e.Property == PageWidthProperty)
            {
                control.PageWidth = (double)e.NewValue;
                control.Zoom(control._zoommode);
            }
            else if (e.Property == PageHeightProperty)
            {
                control.PageHeight = (double)e.NewValue;
                control.Zoom(control._zoommode);
            }
        }

        private ZoomMode _zoommode = ZoomMode.ZoomToFit;

        private bool _transparent;
        public bool Transparent
        {
            get { return _transparent; }
            set
            {
                if (value)
                {
                    Scrollview.Background = Brushes.Transparent;
                    Surface.Background = Brushes.Transparent;
                }
                _transparent = value;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Surface.UseLayoutRounding = true;
            Boxview.Width = Surface.Width = _schemeWidth;
            Boxview.Height = Surface.Height = _schemeHeight;
            PreviewKeyDown += DrawingPage_PreviewKeyDown;
            Scrollview.PreviewKeyDown += (o, args) => { args.Handled = !_runtimeMode; };
            Focusable = true;
            Zoom(ZoomMode.Scale100);
        }

        void DrawingPage_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_runtimeMode) return;

            if (!e.IsDown || _sellist.Count <= 0) return;
            var deleted = false;
            foreach (var fig in _sellist)
            {
                var pointDragged = new Point(fig.Left, fig.Top);
                switch (e.Key)
                {
                    case Key.Delete:
                        _figlist.Remove(fig);
                        Surface.DeleteVisual(fig.Visual);
                        deleted = true;
                        break;
                    case Key.Up:
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                        {
                            if (!fig.LockHeight)
                            {
                                fig.Height -= 1;
                                fig.SetControlSize(fig.Width, fig.Height - 1);
                            }
                        }
                        else if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                            pointDragged.Y -= 10;
                        else
                            pointDragged.Y -= 1;
                        break;
                    case Key.Down:
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                        {
                            if (!fig.LockHeight)
                            {
                                fig.Height += 1;
                                fig.SetControlSize(fig.Width, fig.Height + 1);
                            }
                        }
                        else if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                            pointDragged.Y += 10;
                        else
                            pointDragged.Y += 1;
                        break;
                    case Key.Left:
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                        {
                            if (!fig.LockWidth)
                            {
                                fig.Width -= 1;
                                fig.SetControlSize(fig.Width - 1, fig.Height);
                            }
                        }
                        else if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                            pointDragged.X -= 10;
                        else
                            pointDragged.X -= 1;
                        break;
                    case Key.Right:
                        if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                        {
                            if (!fig.LockWidth)
                            {
                                fig.Width += 1;
                                fig.SetControlSize(fig.Width + 1, fig.Height);
                            }
                        }
                        else if (e.KeyboardDevice.Modifiers != ModifierKeys.Control)
                            pointDragged.X += 10;
                        else
                            pointDragged.X += 1;
                        break;
                }
                fig.Drawme(pointDragged);
                if (ContentChanged != null) ContentChanged(this, new EventArgs());
            }
            DoUpdateInfo();
            if (deleted)
            {
                ClearSelection();
                if (ContentChanged != null) ContentChanged(this, new EventArgs());
            }
            DrawSelectionSquares();
        }

        private bool _isDragging;
        private readonly List<Fig> _sellist = new List<Fig>();
        private Fig _selfig;

        public Fig SelectedFig { get { return _selfig; }}

        public IEnumerable<Fig> SelectedList { get { return _sellist; } }

        private readonly Hashtable _figlist = new Hashtable();

        public DrawingCanvas GetCanvas { get { return Surface; } }

        private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_runtimeMode) return;

            if (e.ClickCount > 1) return; // защита от двойного щелчка

            var pointClicked = e.GetPosition(Surface);

            if (EditMode == EditMode.SelectMove || EditMode == EditMode.SelectMultiple)
            {
                // Определение режима
                var visual = Surface.GetVisual(pointClicked);
                EditMode = visual != null ? EditMode.SelectMove : EditMode.SelectMultiple;
            }
            ZoomMode zoom;
            switch (EditMode)
            {
                case EditMode.ZoomIn:
                    zoom = _zoommode;
                    if (zoom > ZoomMode.Scale400 && OnZoomIn != null)
                        OnZoomIn(this, null);
                    break;
                case EditMode.ZoomOut:
                    zoom = _zoommode;
                    if (zoom < ZoomMode.Scale30 && OnZoomOut != null)
                        OnZoomOut(this, null);
                    break;
                case EditMode.AddRectangle:
                case EditMode.AddCircle:
                case EditMode.AddLabel:
                case EditMode.AddTextbox:
                case EditMode.AddHyperlink:
                case EditMode.AddCheckbox:
                case EditMode.AddButton:
                case EditMode.AddCombobox:
                case EditMode.AddImage:
                case EditMode.AddShape:
                    SetAddFigureCursor();
                    ClearSelection(true);
                    _selectionSquare = new DrawingVisual();
                    Surface.AddVisual(_selectionSquare);
                    _selectionSquareTopLeft = pointClicked;
                    _isAddingFigure = true;
                    // Гарантировать получение события MouseLeftButtonUP, даже если пользователь вышел за пределы Canvas.
                    Surface.CaptureMouse();
                    break;
                case EditMode.SelectMove:

                    #region При выборе или перемещении

                    var visual = Surface.GetVisual(pointClicked);
                    if (visual != null)
                    {
                        var fig = (Fig) _figlist[visual];
                        if (fig != null) // фигура
                        {
                            _isDragging = true;
                            SetCursorTo("sarrows.cur"); 
                            if (!_sellist.Contains(fig))
                            {
                                // Выбор изменился. Очистить предыдущий выбор.
                                if (!_sellist.Contains(fig))
                                {
                                    ClearSelection(true);
                                    _sellist.Add(fig);
                                    _selfig = fig;
                                    DoUpdateInfo();
                                    fig.Drawme();
                                    CreateSelectionSquares();
                                }
                            }
                            else if (fig != _selfig)
                            {
                                _selfig = fig;
                                DoUpdateInfo();
                                fig.Drawme();
                            }
                            foreach (var item in _sellist)
                            {
                                var topLeftCorner = new Point(
                                    item.Visual.ContentBounds.TopLeft.X,
                                    item.Visual.ContentBounds.TopLeft.Y);
                                item.ClickOffset = topLeftCorner - pointClicked;
                            }
                            _selfig = fig;
                        }
                        else // маркер
                        {
                            var marker = visual as SizeMarker;
                            if (marker != null && !marker.Locked) // маркер размера
                            {
                                if (marker.Fig != _selfig)
                                {
                                    _selfig = marker.Fig;
                                    DoUpdateInfo();
                                    marker.Fig.Drawme();
                                }
                                _selectionSquare = new DrawingVisual();
                                Surface.AddVisual(_selectionSquare);
                                _selectionSquareTopLeft = marker.ClickOffset;
                                _isSelectedSizing = true;
                                _sizeMarker = marker;
                                Surface.CaptureMouse();
                            }
                        }
                        DrawSelectionSquares();
                    }
                    else
                    {
                        // добавлено мною
                        ClearSelection(true);
                    }

                    #endregion

                    break;
                case EditMode.SelectMultiple:

                    #region При множественном выборе рамкой

                    _selectionSquare = new DrawingVisual();
                    Surface.AddVisual(_selectionSquare);
                    _selectionSquareTopLeft = pointClicked;
                    _isMultiSelecting = true;
                    // Гарантировать получение события MouseLeftButtonUP, даже если пользователь вышел за пределы Canvas.
                    Surface.CaptureMouse();
                    ClearSelection(true);

                    #endregion

                    break;
            }
        }

        public Fig GetElementAt(Point pointClicked)
        {
            var visual = Surface.GetVisual(pointClicked);
            if (visual != null)
                return (Fig) _figlist[visual];
            return null;
        }

        private void SetAddFigureCursor()
        {
            switch (EditMode)
            {
                case EditMode.ZoomIn:
                case EditMode.ZoomOut:
                    SetCursorTo("magnify.cur");
                    break;
                case EditMode.AddRectangle:
                    SetCursorTo("rect-pro.cur");
                    break;
                case EditMode.AddCircle:
                    SetCursorTo("ellipse-pro.cur");
                    break;
                default:
                    SetCursorTo("cross_r.cur");
                    break;
            }
        }

        private void ClearSelection(bool skipInfo = false)
        {
            RemoveSelectionSquares();
            foreach (var fig in _sellist)
            {
                fig.Drawme();
            }
            _sellist.Clear();
            _selfig = null;
            if (!skipInfo)
                DoUpdateInfo();
        }

        private readonly Hashtable _selectionSquares = new Hashtable();

        private void CreateSelectionSquares()
        {
            RemoveSelectionSquares();
            foreach (var fig in _sellist)
            {
                var arr = new SizeMarker[8];
                var sizeLocked = fig.LockWidth && fig.LockHeight;
                for (var i = 0; i < 8; i++)
                {
                    var sqr = new SizeMarker{ Fig = fig, Index = i, Locked = sizeLocked};
                    switch (i)
                    {
                        case 0:
                            sqr.ClickOffset = new Point(fig.Left + fig.Width, fig.Top + fig.Height);
                            break;
                        case 1:
                            sqr.ClickOffset = new Point(fig.Left + fig.Width, fig.Top + fig.Height);
                            break;
                        case 2:
                            sqr.ClickOffset = new Point(fig.Left, fig.Top + fig.Height);
                            break;
                        case 3:
                            sqr.ClickOffset = new Point(fig.Left, fig.Top);
                            break;
                        case 4:
                            sqr.ClickOffset = new Point(fig.Left, fig.Top);
                            break;
                        case 5:
                            sqr.ClickOffset = new Point(fig.Left, fig.Top);
                            break;
                        case 6:
                            sqr.ClickOffset = new Point(fig.Left + fig.Width, fig.Top);
                            break;
                        case 7:
                            sqr.ClickOffset = new Point(fig.Left + fig.Width, fig.Top + fig.Height);
                            break;
                    }
                    arr[i] = sqr;
                    Surface.AddVisual(sqr);
                }
                _selectionSquares.Add(fig, arr);
            }
            DrawSelectionSquares();
        }

        private void DrawSelectionSquares()
        {   
            foreach (var fig in _sellist)
            {
                var arr = (SizeMarker[]) _selectionSquares[fig];
                if (arr == null) continue;
                var vec = new Vector(3 * _zkf, 3 * _zkf);
                var size = new Size(6 * _zkf, 6 * _zkf);
                var isCurrent = _selfig != null && _selfig.Equals(fig);
                var curPen = new Pen(Brushes.Black, 1*_zkf);
                var otherPen = new Pen(Brushes.White, 1*_zkf);
                var lockedPen = new Pen(Brushes.DimGray, 1 * _zkf);
                for (var i = 0; i < 8; i++)
                {
                    var sqr = arr[i];
                    using (var dc = sqr.RenderOpen())
                    {
                        var pt = new Point(fig.Left, fig.Top);
                        switch (i)
                        {
                            case 0:
                                pt = pt - vec;
                                break;
                            case 1:
                                pt = pt - vec + new Vector(Math.Round(fig.Width / 2), 0);
                                break;
                            case 2:
                                pt = pt - vec + new Vector(fig.Width, 0);
                                break;
                            case 3:
                                pt = pt - vec + new Vector(fig.Width, Math.Round(fig.Height / 2));
                                break;
                            case 4:
                                pt = pt - vec + new Vector(fig.Width, fig.Height);
                                break;
                            case 5:
                                pt = pt - vec + new Vector(Math.Round(fig.Width / 2), fig.Height);
                                break;
                            case 6:
                                pt = pt - vec + new Vector(0, fig.Height);
                                break;
                            case 7:
                                pt = pt - vec + new Vector(0, Math.Round(fig.Height / 2));
                                break;
                        }
                        if (sqr.Locked)
                            dc.DrawRectangle(Brushes.DimGray, lockedPen, new Rect(pt, size));
                        else if (isCurrent)
                            dc.DrawRectangle(Brushes.White, curPen, new Rect(pt, size));
                        else
                            dc.DrawRectangle(Brushes.Black, otherPen, new Rect(pt, size));
                    }
                }
            }
        }

        private void RemoveSelectionSquares()
        {
            foreach (var fig in _sellist)
            {
                var arr = (SizeMarker[]) _selectionSquares[fig];
                if (arr == null) continue;
                foreach (var sqr in arr)
                    Surface.DeleteVisual(sqr);
                _selectionSquares.Remove(fig);
            }
        }

        private DrawingVisual _selectionSquare;
        private Point _selectionSquareTopLeft;
        private bool _isMultiSelecting;
        private bool _isAddingFigure;
        private bool _isSelectedMoving;
        private bool _isSelectedSizing;
        private SizeMarker _sizeMarker;

        private void Surface_MouseMove(object sender, MouseEventArgs e)
        {
            if (_runtimeMode) return;

            if (_isDragging && _sellist.Count > 0)
            {
                SetCursorTo("sarrows.cur");
                if (!_isSelectedMoving)
                {
                    _isSelectedMoving = true;
                    RemoveSelectionSquares();
                }
                var pt = e.GetPosition(Surface);
                var dragged = false;
                foreach (var fig in _sellist)
                {
                    if (fig.ClickOffset.Length > 0) dragged = true;
                    var pointDragged = pt + fig.ClickOffset +
                                       new Vector(fig.StrokeThickness/2, fig.StrokeThickness/2);

                    pointDragged.X = Math.Truncate(pointDragged.X) + 0.5;
                    pointDragged.Y = Math.Truncate(pointDragged.Y) + 0.5;
    
                    fig.Drawme(pointDragged);
                }
                DrawSelectionSquares();
                if (ContentChanged != null && dragged)
                {
                    ContentChanged(this, new EventArgs());
                    DoUpdateInfo();
                }
            }
            else if (_isMultiSelecting || _isAddingFigure)
            {
                if (_isMultiSelecting)
                    SetCursorTo("21783.cur");
                var pointDragged = e.GetPosition(Surface);
                DrawSelectionSquare(_selectionSquareTopLeft, pointDragged);
            }
            else if (_isSelectedSizing)
            {
                if (!_isSelectedMoving)
                {
                    _isSelectedMoving = true;
                    RemoveSelectionSquares();
                }
                var pt = e.GetPosition(Surface);
                switch (_sizeMarker.Index)
                {
                    case 1:
                        pt.X = _selfig.Left;
                        break;
                    case 3:
                        pt.Y = _selfig.Top + _selfig.Height;
                        break;
                    case 5:
                        pt.X = _selfig.Left + _selfig.Width;
                        break;
                    case 7:
                        pt.Y = _selfig.Top;
                        break;
                }
                DrawSelectionSquare(_selectionSquareTopLeft, pt);
            }
            else
            {
                if (EditMode == EditMode.SelectMove || EditMode == EditMode.SelectMultiple)
                {
                    var pointClicked = e.GetPosition(Surface);
                    // Определение режима
                    var visual = Surface.GetVisual(pointClicked);
                    if (visual != null)
                    {
                        if (_figlist.ContainsKey(visual))
                            SetCursorTo("CRMOVEALLARROW.cur");
                        else
                        {
                            var marker = visual as SizeMarker;
                            if (marker != null && !marker.Locked)
                            {
                                switch (marker.Index)
                                {
                                    case 0:
                                    case 4:
                                        Cursor = Cursors.SizeNWSE;
                                        break;
                                    case 1:
                                    case 5:
                                        Cursor = Cursors.SizeNS;
                                        break;
                                    case 2:
                                    case 6:
                                        Cursor = Cursors.SizeNESW;
                                        break;
                                    case 3:
                                    case 7:
                                        Cursor = Cursors.SizeWE;
                                        break;
                                }
                            }
                            else
                                Cursor = Cursors.Arrow;
                        }
                    }
                    else
                        Cursor = Cursors.Arrow;
                }
                else
                    SetAddFigureCursor();
            }
        }

        private void SetCursorTo(string cursor)
        {
            var sri = Application.GetResourceStream(new Uri("cursors\\" + cursor, UriKind.Relative));
            if (sri == null) return;
            var customCursor = new Cursor(sri.Stream);
            Cursor = customCursor;
        }

        private readonly Brush _selectionSquareBrush = Brushes.Transparent;
        private readonly Pen _selectionSquarePen = new Pen(Brushes.Black, 1);

        private void DrawSelectionSquare(Point point1, Point point2)
        {
            _selectionSquarePen.DashStyle = DashStyles.Dash;
            point1.X = Math.Truncate(point1.X) + 0.5;
            point1.Y = Math.Truncate(point1.Y) + 0.5;
            point2.X = Math.Truncate(point2.X) + 0.5;
            point2.Y = Math.Truncate(point2.Y) + 0.5;
            using (var dc = _selectionSquare.RenderOpen())
            {
                dc.DrawRectangle(_selectionSquareBrush, _selectionSquarePen, new Rect(point1, point2));
            }
        }

        private void Surface_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_runtimeMode) return;
              
            if (_isDragging && _isSelectedMoving)
            {
                _isSelectedMoving = false;
                CreateSelectionSquares();
                SetCursorTo("CRMOVEALLARROW.cur");
            }
            else
                if (_isAddingFigure)
                    SetAddFigureCursor();
                else
                    switch (EditMode)
                    {
                        case EditMode.ZoomIn:
                        case EditMode.ZoomOut:
                            SetCursorTo("magnify.cur");
                            break;
                        default:
                            Cursor = Cursors.Arrow;
                            break;
                    }
            _isDragging = false;
            if (_isMultiSelecting)
            {
                var geometry = new RectangleGeometry(
                    new Rect(_selectionSquareTopLeft, e.GetPosition(Surface)));
                var visualsInRegion = Surface.GetVisuals(geometry);
                ClearSelection(true);
                foreach (var fig in visualsInRegion
                    .Select(drawingVisual => (Fig) _figlist[drawingVisual])
                    .Where(fig => fig != null))
                {
                    _sellist.Add(fig);
                    fig.Drawme();
                }
                if (_sellist.Count > 0) _selfig = _sellist[0];
                _isMultiSelecting = false;
                Surface.DeleteVisual(_selectionSquare);
                Surface.ReleaseMouseCapture();
                CreateSelectionSquares();
                DoUpdateInfo();
            }
            else if (_isAddingFigure)
            {
                var geometry = new RectangleGeometry(
                    new Rect(_selectionSquareTopLeft, e.GetPosition(Surface)));
                #region При добавлении элемента

                Fig fig;
                string folder;
                OpenFileDialog dlg;
                bool? result;
                string filename;
                string destname;
                switch (EditMode)
                {
                    case EditMode.AddRectangle:
                        fig = new Fig(FigKind.Rectangle);
                        break;
                    case EditMode.AddCircle:
                        fig = new Fig(FigKind.Circle) { GradientMode = GradientMode.RadialGradient };
                        break;
                    case EditMode.AddHyperlink:
                        fig = new Fig(FigKind.Hyperlink);
                        fig.SetControlSize(64, 26);
                        break;
                    case EditMode.AddLabel:
                        fig = new Fig(FigKind.Label);
                        fig.SetControlSize(64, 26);
                        break;
                    case EditMode.AddTextbox:
                        fig = new Fig(FigKind.Textbox);
                        fig.SetControlSize(120, 23);
                        break;
                    case EditMode.AddCheckbox:
                        fig = new Fig(FigKind.Checkbox);
                        fig.SetControlSize(72, 16);
                        break;
                    case EditMode.AddButton:
                        fig = new Fig(FigKind.Button) {TextAlignment = FigHorizontalAlignment.Center};
                        fig.SetControlSize(75, 22);
                        break;
                    case EditMode.AddCombobox:
                        fig = new Fig(FigKind.Combobox);
                        fig.SetControlSize(120, 22);
                        break;
                    case EditMode.AddImage:
                        // Configure open file dialog box
                        folder = _imagefolder ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
                        dlg = new OpenFileDialog
                            {
                                InitialDirectory = folder,
                                FileName = "",
                                DefaultExt = ".png",
                                Filter = "Файлы картинок (.png,.jpg,.bmp,.emf,.wmf,.gif)|*.png;*.jpg;*.bmp;*.emf;*.wmf;*.gif"
                            };
                        // Show open file dialog box
                        result = dlg.ShowDialog();
                        // Process open file dialog box results
                        if (result != true) goto escaped;
                        _imagefolder = Path.GetDirectoryName(dlg.FileName);
                        // Open document
                        filename = Path.GetFileName(dlg.FileName) ?? "";
                        if (String.IsNullOrWhiteSpace(filename)) goto escaped;
                        destname = Path.Combine(GetFilesFolder(), filename);
                        if (!File.Exists(destname))
                            File.Copy(dlg.FileName, destname);
                        fig = new Fig(FigKind.Imagelink) { Text = destname };
                        break;
                    case EditMode.AddShape:
                        // Configure open file dialog box
                        folder = _shapefolder ?? HomeFolder;
                        dlg = new OpenFileDialog
                            {
                                InitialDirectory = folder,
                                FileName = "",
                                DefaultExt = ".shp",
                                Filter = "Файлы фигур (.shp)|*.shp"
                            };
                        // Show open file dialog box
                        result = dlg.ShowDialog();
                        // Process open file dialog box results
                        if (result != true) goto escaped;
                        _shapefolder = Path.GetDirectoryName(dlg.FileName);
                        // Open document
                        filename = Path.GetFileName(dlg.FileName) ?? "";
                        if (String.IsNullOrWhiteSpace(filename)) goto escaped;
                        destname = Path.Combine(GetFilesFolder(), filename);
                        if (File.Exists(destname))
                            File.Delete(destname);
                        if (!File.Exists(destname))
                            File.Copy(dlg.FileName, destname);
                        var fromDir = GetLocalFolder(dlg.FileName);
                        var toDir = GetLocalFolder(destname);
                        RemoveDir(toDir);
                        CopyDir(fromDir, toDir);
                        fig = new Fig(FigKind.Shapelink) { Text = filename, LockWidth = true, LockHeight = true };
                        var size = ((DrawingPage)fig.Control).LoadShape(destname);
                        if (!size.IsEmpty)
                            fig.SetControlSize(size.Width, size.Height);
                        break;
                    default:
                        goto escaped;
                }
                fig.Parent = this;
                _selfig = fig;
                var control = fig.Control;
                if (control != null)
                {
                    fig.Width = control.Width;
                    fig.Height = control.Height;
                }
                else
                {
                    fig.Width = Math.Round(geometry.Bounds.Width);
                    fig.Height = Math.Round(geometry.Bounds.Height);                    
                }
                Surface.AddVisual(fig.Visual);
                fig.SetOrderLocation(Surface.GetVisualIndex(fig.Visual));
                _figlist[fig.Visual] = fig;
                if (ContentChanged != null) ContentChanged(this, new EventArgs());
                _sellist.Add(fig);
                var pt = new Point(geometry.Bounds.Left, geometry.Bounds.Top);

                pt.X = Math.Truncate(pt.X) + 0.5;
                pt.Y = Math.Truncate(pt.Y) + 0.5;

                fig.GetFilesFolder = GetFilesFolder;
                fig.Drawme(pt);
                CreateSelectionSquares();
                DoUpdateInfo(); 
                fig.PropertyChanged += fig_PropertyChanged;
                #endregion
            escaped:
                _isAddingFigure = false;
                Surface.DeleteVisual(_selectionSquare);
                Surface.ReleaseMouseCapture();
            }
            else if (_isSelectedSizing)
            {
                _isSelectedSizing = false;
                _isSelectedMoving = false;
                Surface.DeleteVisual(_selectionSquare);
                Surface.ReleaseMouseCapture();
                if (!_selectionSquare.ContentBounds.IsEmpty)
                {
                    var dw = _selectionSquare.ContentBounds.Width - _selfig.Width;
                    if (_selfig.LockWidth) 
                        dw = 0;
                    var dh = _selectionSquare.ContentBounds.Height - _selfig.Height;
                    if (_selfig.LockHeight) 
                        dh = 0;
                    var dx = _selectionSquare.ContentBounds.Left - _selfig.Left;
                    var dy = _selectionSquare.ContentBounds.Top - _selfig.Top;
                    if (!_selfig.LockWidth) 
                        _selfig.Width = _selectionSquare.ContentBounds.Width;
                    if (!_selfig.LockHeight)
                        _selfig.Height = _selectionSquare.ContentBounds.Height;
                    var topleft = _selectionSquare.ContentBounds.TopLeft;

                    topleft.X = Math.Truncate(topleft.X) + 0.5;
                    topleft.Y = Math.Truncate(topleft.Y) + 0.5;

                    _selfig.Drawme(topleft);
                    foreach (var fig in _sellist.Where(fig => fig != _selfig))
                    {
                        if (!fig.LockWidth)
                            fig.Width += dw;
                        if (!fig.LockHeight)
                            fig.Height += dh;
                        var pt = new Point(fig.Left + dx, fig.Top + dy);

                        pt.X = Math.Truncate(pt.X) + 0.5;
                        pt.Y = Math.Truncate(pt.Y) + 0.5;

                        fig.Drawme(pt);
                    }
                }
                //------------------------------------------
                CreateSelectionSquares();
            }
        }

        private static string GetLocalFolder(string filename)
        {
            return Path.Combine(Path.GetDirectoryName(filename) ?? "", Path.GetFileNameWithoutExtension(filename) + "_files");
        }

        private static void CopyDir(string fromDir, string toDir)
        {
            if (!Directory.Exists(fromDir)) return;
            if (!Directory.Exists(toDir))
                Directory.CreateDirectory(toDir);
            foreach (var s1 in Directory.GetFiles(fromDir))
            {
                var s2 = Path.Combine(toDir, Path.GetFileName(s1) ?? "");
                if (!File.Exists(s2))
                    File.Copy(s1, s2);
            }
            foreach (var s in Directory.GetDirectories(fromDir))
            {
                CopyDir(s, Path.Combine(toDir, Path.GetFileName(s) ?? ""));
            }
        }

        private static void RemoveDir(string dir)
        {
            if (!Directory.Exists(dir)) return;
            var folders = Directory.GetDirectories(dir);
            foreach (var s in folders)
                RemoveDir(s);
            var files = Directory.GetFiles(dir);
            foreach (var file in files)
                File.Delete(file);
            Directory.Delete(dir);
        }

        public string GetFilesFolder()
        {
            if (FileName == null) return "";
            var path = GetLocalFolder(FileName);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return path;
        }

        #region Работа с группами

        public void Ungrouping()
        {
            if (_sellist.Count < 1) return;
            var list = new List<Fig>();
            foreach (var asgroup in _sellist.Where(item => item.Group != null))
            {
                _figlist.Remove(asgroup);
                Surface.DeleteVisual(asgroup.Visual);
                foreach (var fig in asgroup.Group)
                {
                    list.Add(fig);
                    Surface.AddVisual(fig.Visual);
                    fig.Index = Surface.GetVisualIndex(fig.Visual);
                    _figlist[fig.Visual] = fig;
                    fig.Left += asgroup.Left;
                    fig.Top += asgroup.Top;
                    fig.Drawme();
                }
            }
            foreach (var fig in _sellist.Where(fig => fig.Group == null))
            {
                list.Add(fig);
                fig.Drawme();
            }
            RemoveSelectionSquares();
            _sellist.Clear();
            foreach (var fig in list.OrderByDescending(fig => fig.Index))
                _sellist.Add(fig);
            _selfig = _sellist.Count > 0 ? _sellist[0] : null;
            CreateSelectionSquares();
            DoUpdateInfo();
            if (ContentChanged != null) ContentChanged(this, new EventArgs());
        }

        public void Grouping()
        {
            if (_sellist.Count(fig => !fig.IsControl) < 2) return;
            var group = new Fig();
            group.Parent = this;
            var left = double.MaxValue;
            var top = double.MaxValue;
            var right = double.MinValue;
            var bottom = double.MinValue;
            foreach (var fig in _sellist.Where(fig => !fig.IsControl))
            {
                _figlist.Remove(fig);
                Surface.DeleteVisual(fig.Visual);
                if (fig.Left < left) left = fig.Left;
                if (fig.Top < top) top = fig.Top;
                if (fig.Left + fig.Width > right) right = fig.Left + fig.Width;
                if (fig.Top + fig.Height > bottom) bottom = fig.Top + fig.Height;
            }
            group.Left = left;
            group.Top = top;
            group.Width = right - left;
            group.Height = bottom - top;
            group.Group = _sellist.Where(fig => !fig.IsControl).OrderBy(item => item.Index).ToArray();
            Surface.AddVisual(group.Visual);
            group.Index = Surface.GetVisualIndex(group.Visual);
            _figlist[group.Visual] = group;
            foreach (var fig in group.Group)
            {
                fig.Left -= left;
                fig.Top -= top;
            }
            RemoveSelectionSquares();
            _sellist.Clear();
            _sellist.Add(group);
            _selfig = group;
            DoUpdateInfo();
            group.Drawme(new Point(left, top));
            CreateSelectionSquares();
            if (ContentChanged != null) ContentChanged(this, new EventArgs());
        }

        #endregion

        private void DoUpdateInfo()
        {
            if (SelectUpdated != null)
                SelectUpdated(this,
                              new SelectUpdatedEventArgs
                                  {
                                      HasGroups = _sellist.Any(item => item.Group != null),
                                      MoreOneSelected = _sellist.Count > 1,
                                      MoreTwoSelected = _sellist.Count > 2,
                                      HasSelected = _sellist.Count > 0,
                                      List = _sellist,
                                      Item = _selfig
                                  });
        }

        public string FileName { get; private set; }

        public void SaveContent(string filename = null)
        {
            if (filename == null && FileName == null) return;
            filename = filename ?? FileName;
            if (File.Exists(filename)) File.Delete(filename);
            FileName = filename;
            // Получение списка фигур Fig с обновлением индекса фигуры в списке и положения на канве
            var list = new List<Fig>();
            foreach (var fig in _figlist.Keys.Cast<DrawingVisual>()
                                        .Select(visual => (Fig)_figlist[visual]))
            {
                var idx = Surface.GetVisualIndex(fig.Visual);
                if (idx < 0) continue;
                fig.SetOrderLocation(idx);
                list.Add(fig);
            }
            DataContractJsonSerializer json;
            switch (EditorMode)
            {
                case EditorMode.AsScheme:
                    Scheme.Items = list.ToArray();
                    var scheme = Scheme;
                    // Запись списка фигур в файл
                    json = new DataContractJsonSerializer(typeof (Scheme));
                    using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        json.WriteObject(stream, scheme);
                    }
                    Scheme.IsSaved = true;
                    break;
                case EditorMode.AsShape:
                    Shape.Items = list.ToArray();
                    var shape = Shape;
                    // Запись списка фигур в файл
                    json = new DataContractJsonSerializer(typeof(Shape));
                    using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        json.WriteObject(stream, shape);
                    }
                    break;
            }
        }

        private void EmptyContent()
        {
            ClearSelection();
            _figlist.Clear();
            Surface.Clear();
        }

        public void CreateScheme()
        {
            EditorMode = EditorMode.AsScheme;
            EmptyContent();
            Scrollview.Background = new SolidColorBrush(SystemColors.AppWorkspaceColor);
            Surface.Background = Brushes.Silver;
            PageFillColor = Colors.Silver;
            FileName = null;
            Scheme = new Scheme { Width = 1920, Height = 1080, FillColor = Colors.Silver, BackgroundImageUri = null};
            Shape = null;
            Zoom(ZoomMode.Scale100);
            SetBindingSize(Scheme);
        }

        public void CreateShape()
        {
            EditorMode = EditorMode.AsShape;
            EmptyContent();
            Scrollview.Background = new SolidColorBrush(SystemColors.AppWorkspaceColor);
            Surface.Background = Brushes.Silver;
            FileName = null;
            Shape = new Shape { Width = 640, Height = 480 };
            Scheme = null;
            Zoom(ZoomMode.Scale100);
            SetBindingSize(Shape);
        }

        #region Загрузка мнемосхем

        public void LoadContentRuntime(string filename)
        {
            _runtimeMode = true;
            EmptyContent();
            Scrollview.Background = new SolidColorBrush(SystemColors.AppWorkspaceColor);
            Surface.Background = Brushes.Silver;
            if (!File.Exists(filename)) return;
            FileName = filename;
            var extension = Path.GetExtension(FileName);
            if (extension != null && extension.Equals(".scm"))
            {
                EditorMode = EditorMode.AsScheme;
            }
            else
                return;
            var json = new DataContractJsonSerializer(EditorMode == EditorMode.AsScheme ? typeof(Scheme) : typeof(Shape));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var scheme = (Scheme)json.ReadObject(stream);
                if (double.IsInfinity(scheme.Width) || Math.Abs(scheme.Width - 0) < 0.0001) scheme.Width = 1920;
                if (double.IsInfinity(scheme.Height) || Math.Abs(scheme.Height - 0) < 0.0001) scheme.Height = 1080;
                var list = scheme.Items;
                _schemeWidth = scheme.Width;
                _schemeHeight = scheme.Height;
                Shape = null;
                Scheme = scheme;
                PageFillColor = Scheme.FillColor;
                SetBindingSize(Scheme);
                Scheme.IsSaved = true;
                if (list != null)
                    foreach (var fig in list.OrderBy(fig => fig.Index))
                    {
                        if (fig.IsControl) fig.CreateControl(true);
                        InitAndConnectFilesFolder(fig);
                        var control = fig.Control;
                        if (control == null)
                        {
                            fig.Drawme(new Point(fig.Left, fig.Top));
                            Surface.AddVisual(fig.Visual);
                        }
                        else
                        {
                            Canvas.SetLeft(control, fig.Left);
                            Canvas.SetTop(control, fig.Top);
                            Surface.Children.Add(control);
                            if (fig.Kind == FigKind.Shapelink)
                            {
                                var shape = (DrawingPage)control;
                                var folder = GetFilesFolder();
                                var shapefile = Path.Combine(folder, fig.Text ?? "");
                                shape.LoadShapeRuntime(shapefile);
                                control.Width = fig.Width * 1.1;
                                control.Height = fig.Height * 1.1;
                                shape.Zoom(ZoomMode.ZoomToFit);
                                shape.InvalidateVisual();
                                
                            }
                        }
                        _figlist[fig.Visual] = fig;
                        fig.PropertyChanged += fig_PropertyChanged;
                    }
            }
            Zoom(_zoommode);
        }

        public void LoadContent(string filename)
        {
            EmptyContent();
            Scrollview.Background = new SolidColorBrush(SystemColors.AppWorkspaceColor);
            Surface.Background = Brushes.Silver;
            if (!File.Exists(filename)) return;
            FileName = filename;
            var extension = Path.GetExtension(FileName);
            if (extension != null && extension.Equals(".scm"))
                EditorMode = EditorMode.AsScheme;
            else if (extension != null && extension.Equals(".shp"))
                EditorMode = EditorMode.AsShape;
            else
                return;
            var json = new DataContractJsonSerializer(EditorMode == EditorMode.AsScheme ? typeof(Scheme) : typeof(Shape));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                Fig[] list;
                if (EditorMode == EditorMode.AsScheme)
                {
                    var scheme = (Scheme) json.ReadObject(stream);
                    if (double.IsInfinity(scheme.Width) || Math.Abs(scheme.Width - 0) < 0.0001) scheme.Width = 1920;
                    if (double.IsInfinity(scheme.Height) || Math.Abs(scheme.Height - 0) < 0.0001) scheme.Height = 1080;
                    list = scheme.Items;
                    _schemeWidth = scheme.Width;
                    _schemeHeight = scheme.Height;
                    Shape = null;
                    Scheme = scheme;
                    PageFillColor = Scheme.FillColor;
                    SetBindingSize(Scheme);
                    Scheme.IsSaved = true;
                }
                else
                {
                    var shape = (Shape) json.ReadObject(stream);
                    if (double.IsInfinity(shape.Width) || Math.Abs(shape.Width - 0) < 0.0001) shape.Width = 640;
                    if (double.IsInfinity(shape.Height) || Math.Abs(shape.Height - 0) < 0.0001) shape.Height = 480;
                    list = shape.Items;
                    _schemeWidth = shape.Width;
                    _schemeHeight = shape.Height;
                    Scheme = null;
                    Shape = shape;
                    SetBindingSize(Shape);
                }
                if (list != null)
                    foreach (var fig in list.OrderBy(fig => fig.Index))
                    {
                        if (fig.IsControl) fig.CreateControl(true);
                        InitAndConnectFilesFolder(fig);
                        fig.Drawme(new Point(fig.Left, fig.Top));
                        Surface.AddVisual(fig.Visual);
                        _figlist[fig.Visual] = fig;
                        fig.PropertyChanged += fig_PropertyChanged;
                    }
            }
            Zoom(_zoommode);
        }

        #endregion

        private void fig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ContentChanged != null) ContentChanged(this, new EventArgs());
        }

        private void InitAndConnectFilesFolder(Fig fig)
        {
            fig.Parent = this;
            fig.GetFilesFolder = GetFilesFolder;
            if (fig.Group != null)
                foreach (var item in fig.Group)
                    InitAndConnectFilesFolder(item);
        }

        #region Загрузка шейпов

        public Size LoadShapeRuntime(string filename)
        {
            _runtimeMode = true;
            EmptyContent();
            if (!File.Exists(filename)) return new Size();
            FileName = filename;
            var extension = Path.GetExtension(FileName);
            if (extension != null && extension.Equals(".shp"))
            {
                EditorMode = EditorMode.AsShape;
            }
            else
                return new Size();
            var json = new DataContractJsonSerializer(typeof(Shape));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var shape = (Shape)json.ReadObject(stream);
                if (double.IsInfinity(shape.Width) || Math.Abs(shape.Width - 0) < 0.0001) shape.Width = 640;
                if (double.IsInfinity(shape.Height) || Math.Abs(shape.Height - 0) < 0.0001) shape.Height = 480;
                var list = shape.Items;
                #region коррекция по содержимому

                if (list.Length > 0)
                {
                    var left = double.MaxValue;
                    var top = double.MaxValue;
                    var right = double.MinValue;
                    var bottom = double.MinValue;
                    foreach (var fig in list)
                    {
                        if (fig.Left < left) left = fig.Left - fig.StrokeThickness / 2;
                        if (fig.Top < top) top = fig.Top - fig.StrokeThickness / 2;
                        if (fig.Left + fig.Width > right) right = fig.Left + fig.Width + fig.StrokeThickness / 2;
                        if (fig.Top + fig.Height > bottom) bottom = fig.Top + fig.Height + fig.StrokeThickness / 2;
                    }
                    foreach (var fig in list)
                    {
                        fig.Left -= left;
                        fig.Top -= top;
                    }
                    _schemeWidth = shape.Width = right - left;
                    _schemeHeight = shape.Height = bottom - top;
                }
                else
                {
                    _schemeWidth = shape.Width;
                    _schemeHeight = shape.Height;
                }

                #endregion коррекция по содержимому
                Scheme = null;
                Shape = shape;
                SetBindingSize(Shape);
                foreach (var fig in list.OrderBy(fig => fig.Index))
                {
                    if (fig.IsControl) fig.CreateControl(true);
                    fig.GetFilesFolder = GetFilesFolder;
                        var control = fig.Control;
                    if (control == null)
                    {
                        fig.Drawme(new Point(fig.Left, fig.Top));
                        Surface.AddVisual(fig.Visual);
                    }
                    else
                    {
                        Canvas.SetLeft(control, fig.Left);
                        Canvas.SetTop(control, fig.Top);
                        Surface.Children.Add(control);
                        if (fig.Kind == FigKind.Shapelink)
                        {
                            var childShape = (DrawingPage)control;
                            var folder = GetFilesFolder();
                            var shapefile = Path.Combine(folder, fig.Text ?? "");
                            childShape.LoadShapeRuntime(shapefile);
                            control.Width = fig.Width;
                            control.Height = fig.Height;
                            childShape.Zoom(ZoomMode.ZoomToFit);
                            childShape.InvalidateVisual();
                        }
                    }
                    _figlist[fig.Visual] = fig;
                }
            }
            Zoom(ZoomMode.Scale100);
            return new Size(_schemeWidth, _schemeHeight);
        }

        public Size LoadShape(string filename)
        {
            EmptyContent();
            if (!File.Exists(filename)) return new Size();
            FileName = filename;
            var extension = Path.GetExtension(FileName);
            if (extension != null && extension.Equals(".shp"))
                EditorMode = EditorMode.AsShape;
            else
                return new Size();
            var json = new DataContractJsonSerializer(typeof(Shape));
            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                var shape = (Shape) json.ReadObject(stream);
                if (double.IsInfinity(shape.Width) || Math.Abs(shape.Width - 0) < 0.0001) shape.Width = 640;
                if (double.IsInfinity(shape.Height) || Math.Abs(shape.Height - 0) < 0.0001) shape.Height = 480;
                var list = shape.Items;
                #region коррекция по содержимому

                if (list.Length > 0)
                {
                    var left = double.MaxValue;
                    var top = double.MaxValue;
                    var right = double.MinValue;
                    var bottom = double.MinValue;
                    foreach (var fig in list)
                    {
                        if (fig.Left < left) left = fig.Left - fig.StrokeThickness;
                        if (fig.Top < top) top = fig.Top - fig.StrokeThickness;
                        if (fig.Left + fig.Width > right) right = fig.Left + fig.Width + fig.StrokeThickness;
                        if (fig.Top + fig.Height > bottom) bottom = fig.Top + fig.Height + fig.StrokeThickness;
                    }
                    foreach (var fig in list)
                    {
                        fig.Left -= left;
                        fig.Top -= top;
                    }
                    _schemeWidth = shape.Width = right - left;
                    _schemeHeight = shape.Height = bottom - top;
                }
                else
                {
                    _schemeWidth = shape.Width;
                    _schemeHeight = shape.Height;
                }

                #endregion коррекция по содержимому
                Scheme = null;
                Shape = shape;
                SetBindingSize(Shape);
                foreach (var fig in list.OrderBy(fig => fig.Index))
                {
                    if (fig.IsControl) fig.CreateControl(true);
                    fig.GetFilesFolder = GetFilesFolder;
                    fig.Drawme(new Point(fig.Left, fig.Top));
                    Surface.AddVisual(fig.Visual);
                    _figlist[fig.Visual] = fig;
                }
            }
            Zoom(ZoomMode.Scale100);
            return new Size(_schemeWidth, _schemeHeight);
        }

        #endregion

        private double _zkf = 1.0;

        #region Масштабирование

        public void Zoom(ZoomMode mode)
        {
            _zoommode = mode;
            switch (mode)
            {
                case ZoomMode.Scale400:
                case ZoomMode.Scale200:
                case ZoomMode.Scale150:
                case ZoomMode.Scale125:
                case ZoomMode.Scale80:
                case ZoomMode.Scale60:
                case ZoomMode.Scale50:
                case ZoomMode.Scale30:
                    switch (mode)
                    {
                        case ZoomMode.Scale400:
                            Boxview.Width = _schemeWidth * 4;
                            Boxview.Height = _schemeHeight * 4;
                            _zkf = 1.0;
                            break;
                        case ZoomMode.Scale200:
                            Boxview.Width = _schemeWidth * 2;
                            Boxview.Height = _schemeHeight * 2;
                            _zkf = 1.0;
                            break;
                        case ZoomMode.Scale150:
                            Boxview.Width = _schemeWidth * 1.5;
                            Boxview.Height = _schemeHeight * 1.5;
                            _zkf = 1.0;
                            break;
                        case ZoomMode.Scale125:
                            Boxview.Width = _schemeWidth * 1.25;
                            Boxview.Height = _schemeHeight * 1.25;
                            _zkf = 1.0;
                            break;
                        case ZoomMode.Scale80:
                            Boxview.Width = _schemeWidth * 0.8;
                            Boxview.Height = _schemeHeight * 0.8;
                            _zkf = 1/0.8;
                            break;
                        case ZoomMode.Scale60:
                            Boxview.Width = _schemeWidth * 0.6;
                            Boxview.Height = _schemeHeight * 0.6;
                            _zkf = 1/0.6;
                            break;
                        case ZoomMode.Scale50:
                            Boxview.Width = _schemeWidth * 0.5;
                            Boxview.Height = _schemeHeight * 0.5;
                            _zkf = 1/0.5;
                            break;
                        case ZoomMode.Scale30:
                            Boxview.Width = _schemeWidth * 0.3;
                            Boxview.Height = _schemeHeight * 0.3;
                            _zkf = 1/0.3;
                            break;
                    }
                    Boxview.Stretch = Stretch.Uniform;
                    Surface.Width = _schemeWidth;
                    Surface.Height = _schemeHeight;
                    Scrollview.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    Scrollview.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    break;
                case ZoomMode.Scale100:
                    Boxview.Stretch = Stretch.None;
                    Boxview.Width = Surface.Width = _schemeWidth;
                    Boxview.Height = Surface.Height = _schemeHeight;
                    Scrollview.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                    Scrollview.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    _zkf = 1.0;
                    break;
                case ZoomMode.ZoomToFit:
                    Boxview.Stretch = Stretch.Fill;
                    Boxview.Width = Scrollview.ActualWidth;
                    Boxview.Height = Scrollview.ActualHeight;
                    Scrollview.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    Scrollview.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                    _zkf = 1.0;
                    break;
            }     
            DrawSelectionSquares();
        }

        #endregion

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_zoommode == ZoomMode.ZoomToFit) Zoom(ZoomMode.ZoomToFit);
            Surface.Focus();
        }

        public void UpdateSelected()
        {
            foreach (var fig in _sellist)
                fig.Drawme();
            RemoveSelectionSquares();
            CreateSelectionSquares();
        }

        public void BringToFront()
        {
            if (_sellist.Count == 0) return;
            foreach (var fig in _sellist)
                Surface.DeleteVisual(fig.Visual);
            foreach (var fig in _sellist.OrderByDescending(fig => fig.Index))
                Surface.AddVisual(fig.Visual);
            ClearSelection();
        }

        public void SendToBack()
        {
            if (_sellist.Count == 0) return;
            Surface.Clear();
            foreach (var fig in _sellist.OrderByDescending(fig => fig.Index))
                Surface.AddVisual(fig.Visual);
            foreach (var fig in _figlist.Values.Cast<Fig>()
                .Where(fig => !_sellist.Contains(fig))
                .OrderBy(fig => fig.Index))
                Surface.AddVisual(fig.Visual);
            ClearSelection();
        }
    }

    public class SizeMarker : DrawingVisual
    {
        public Fig Fig { get; set; }
        public int Index { get; set; }
        public Point ClickOffset { get; set; }
        public bool Locked { get; set; }
    }
}

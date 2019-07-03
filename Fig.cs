using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplayBuilder.Properties;

namespace WpfDisplayBuilder
{
    public enum FigKind
    {
        Rectangle,
        Circle,
        Checkbox,
        Button,
        Combobox,
        Label,
        Textbox,
        Hyperlink,
        Imagelink,
        Shapelink
    }

    public enum FigVisibility : int
    {
        [LocalizableDescription(@"Inherit", typeof(Resources))] 
        Inherit = 0,
        [LocalizableDescription(@"Visible", typeof(Resources))] 
        Visible = 1,
        [LocalizableDescription(@"Hidden", typeof(Resources))]
        Hidden = 2
    }

    public enum FigDashStyles : int
    {
        [LocalizableDescription(@"Noline", typeof(Resources))]
        Noline,
        [LocalizableDescription(@"Solid", typeof(Resources))]
        Solid,
        [LocalizableDescription(@"Dash", typeof(Resources))]
        Dash,
        [LocalizableDescription(@"DashDot", typeof(Resources))]
        DashDot,
        [LocalizableDescription(@"DashDotDot", typeof(Resources))]
        DashDotDot,
        [LocalizableDescription(@"Dot", typeof(Resources))]
        Dot
    }

    public enum FigHorizontalAlignment : int
    {
        [LocalizableDescription(@"Left", typeof(Resources))]
        Left,
        [LocalizableDescription(@"Right", typeof(Resources))]
        Right,
        [LocalizableDescription(@"Center", typeof(Resources))]
        Center,
        [LocalizableDescription(@"Stretch", typeof(Resources))]
        Stretch
    }


    public delegate string GetFilesFolder();

    [DataContract]
    [DefaultProperty("Name")]
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class Fig : FilterablePropertyBase
    {
        public DrawingPage Parent { get; set; }
        
        public GetFilesFolder GetFilesFolder { private get; set; }

        private const double MinWidth = 1;
        private const double MinHeight = 1;

        #region Свойства

        [Category("Конфигурация"), DisplayName(@"Имя фигуры"), 
        Description("string Name - уникальное в пределах мнемосхемы имя фигуры."), 
        MergableProperty(false), ParenthesizePropertyName(true), 
        DataMember(Name = "id", EmitDefaultValue = false)]
        public string Name { get; set; }

        [DataMember(Name = "tip", EmitDefaultValue = false)]
        public string ToolTip { get; set; }

        [DataMember(Name = "n", EmitDefaultValue = false)]
        public int Index { get; set; }

        [DataMember(Name = "x", EmitDefaultValue = false)]
        public double Left
        {
            get { return _left; }
            set
            {
                var changed = Math.Abs(_left - value) > 0.0001;
                _left = value;
                if (changed)
                    RaisePropertyChanged("Left");
            }
        }

        [DataMember(Name = "y", EmitDefaultValue = false)]
        public double Top
        {
            get { return _top; }
            set
            {
                var changed = Math.Abs(_top - value) > 0.0001;
                _top = value;
                if (changed)
                    RaisePropertyChanged("Top");
            }
        }

        private double _width = MinWidth;

        [DataMember(Name = "w", EmitDefaultValue = false)]
        public double Width
        {
            get { return _width; }
            set
            {
                _width = value >= MinWidth ? value : MinWidth; 
                RaisePropertyChanged("Width");
                SetControlSize(_width, null);
                Drawme();
            }
        }

        [DataMember(Name = "lw", EmitDefaultValue = false)]
        public bool LockWidth { get; set; }

        private double _height = MinHeight;

        [DataMember(Name = "h")]
        public double Height
        {
            get { return _height; }
            set
            {
                _height = value >= MinHeight ? value : MinHeight; 
                RaisePropertyChanged("Height");
                SetControlSize(null, _height);
                Drawme();
            }
        }

        [DataMember(Name = "lh", EmitDefaultValue = false)]
        public bool LockHeight { get; set; }

        [DataMember(Name = "g", EmitDefaultValue = false)]
        public Fig[] Group { get; set; }

        [DataMember(Name = "custs", EmitDefaultValue = false)]
        public CustomProperty[] Customs { get; set; }

        #region Fill

        [DataMember(Name = "fc", EmitDefaultValue = false)]
        public Int32 IntFillColor { get; set; }

// ReSharper disable MemberCanBePrivate.Global
        public Color FillColor
// ReSharper restore MemberCanBePrivate.Global
        {
            get
            {
                var arr = BitConverter.GetBytes(IntFillColor);
                return Color.FromArgb(arr[3], arr[0], arr[1], arr[2]);
            }
            set
            {
                var arr = new[] { value.R, value.G, value.B, value.A };
                IntFillColor = BitConverter.ToInt32(arr, 0);
                RaisePropertyChanged("FillColor");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        [DataMember(Name = "fblk", EmitDefaultValue = false)]
        public Int32 IntFillGradientColor { get; set; }

// ReSharper disable MemberCanBePrivate.Global
        public Color FillGradientColor
// ReSharper restore MemberCanBePrivate.Global
        {
            get
            {
                var arr = BitConverter.GetBytes(IntFillGradientColor);
                return Color.FromArgb(arr[3], arr[0], arr[1], arr[2]);
            }
            set
            {
                var arr = new[] { value.R, value.G, value.B, value.A };
                IntFillGradientColor = BitConverter.ToInt32(arr, 0);
                RaisePropertyChanged("FillGradientColor");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        
        [DataMember(Name = "fm", EmitDefaultValue = false)]
        public FillMode FillMode
        {
            get { return _fillMode; }
            set
            {
                _fillMode = value;
                RaisePropertyChanged("FillMode");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        [DataMember(Name = "gm", EmitDefaultValue = false)]
        public GradientMode GradientMode
        {
            get { return _gradientMode; }
            set
            {
                _gradientMode = value;
                RaisePropertyChanged("GradientMode");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        public bool IsLinearGradient
        {
            get { return FillMode == FillMode.Gradient && GradientMode == GradientMode.LinearGradient; }
        }

        public bool IsGradient { get { return FillMode == FillMode.Gradient; } }

        [DataMember(Name = "fa", EmitDefaultValue = false)]
        public double FillAngle
        {
            get { return _fillAngle; }
            set
            {
                _fillAngle = value;
                RaisePropertyChanged("FillAngle");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        [DataMember(Name = "fo", EmitDefaultValue = false)]
        public double FillOffset
        {
            get { return _fillOffset; }
            set
            {
                _fillOffset = value;
                RaisePropertyChanged("FillOffset");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        private readonly Brush _fillBrush = null;

        private Brush FillBrush
        {
            get
            {
                if (FillMode == FillMode.Gradient && GradientMode == GradientMode.LinearGradient)
                {
                    var gbrush = _fillBrush as LinearGradientBrush;
                    if (gbrush == null)
                    {
                        var coll = new GradientStopCollection
                        {
                            new GradientStop {Color = FillColor, Offset = 0},
                            new GradientStop {Color = FillGradientColor, Offset = FillOffset},
                            new GradientStop {Color = FillColor, Offset = 1}
                        };
                       gbrush = new LinearGradientBrush(coll, FillAngle);
                    }
                    return gbrush;
                }
                if (FillMode == FillMode.Gradient && GradientMode == GradientMode.RadialGradient)
                {
                    var rbrush = _fillBrush as RadialGradientBrush;
                    if (rbrush == null)
                    {
                        rbrush = new RadialGradientBrush();
                        rbrush.GradientStops.Add(new GradientStop {Color = FillGradientColor, Offset = 0});
                        rbrush.GradientStops.Add(new GradientStop {Color = FillColor, Offset = 1});
                        rbrush.GradientOrigin = new Point(0.7, 0.3);
                    }
                    return rbrush;
                }
                if (FillMode == FillMode.Solid)
                {
                    var sbrush = new SolidColorBrush(FillColor);
                    return sbrush;
                }
                return Brushes.Transparent;
            }
        }

        [DataMember(Name = "fcb", EmitDefaultValue = false)]
        public bool FillColorBlink
        {
            get { return _fillColorBlink; }
            set
            {
                _fillColorBlink = value;
                RaisePropertyChanged("FillColorBlink");
                if (IsControl && _control != null)
                    Control.Background = FillBrush;
                Drawme();
            }
        }

        #endregion Fill

        #region Stroke

        [DataMember(Name = "sc", EmitDefaultValue = false)]
        public Int32 IntStrokeColor { get; set; }

// ReSharper disable MemberCanBePrivate.Global
        public Color StrokeColor
// ReSharper restore MemberCanBePrivate.Global
        {
            get
            {
                var arr = BitConverter.GetBytes(IntStrokeColor);
                return Color.FromArgb(arr[3], arr[0], arr[1], arr[2]);
            }
            set
            {
                var arr = new[] { value.R, value.G, value.B, value.A };
                IntStrokeColor = BitConverter.ToInt32(arr, 0);
                RaisePropertyChanged("StrokeColor");
                Drawme();
            }
        }

        [DataMember(Name = "st", EmitDefaultValue = false)]
        public double StrokeThickness
        {
            get { return _strokeThickness; }
            set
            {
                _strokeThickness = value;
                RaisePropertyChanged("StrokeThickness");
                Drawme();
            }
        }

        private Brush StrokeBrush { get { return new SolidColorBrush(StrokeColor); } }

        private Pen StrokePen
        {
            get
            {
                var gpen = new Pen(StrokeBrush, StrokeThickness);
                switch (StrokeDashStyle)
                {
                    case FigDashStyles.Noline:
                    case FigDashStyles.Solid:
                        gpen.DashStyle = DashStyles.Solid;
                        break;
                    case FigDashStyles.Dash:
                        gpen.DashStyle = DashStyles.Dash;
                        break;
                    case FigDashStyles.DashDot:
                        gpen.DashStyle = DashStyles.DashDot;
                        break;
                    case FigDashStyles.DashDotDot:
                        gpen.DashStyle = DashStyles.DashDotDot;
                        break;
                    case FigDashStyles.Dot:
                        gpen.DashStyle = DashStyles.Dot;
                        break;
                }
                return gpen;
            }
        }

        [DataMember(Name = "scb", EmitDefaultValue = false)]
        public bool StrokeColorBlink
        {
            get { return _strokeColorBlink; }
            set
            {
                _strokeColorBlink = value;
                RaisePropertyChanged("StrokeColorBlink");
                Drawme();
            }
        }

        [DataMember(Name = "sls", EmitDefaultValue = false)]
        public FigDashStyles StrokeDashStyle
        {
            get { return _strokeDashStyle; }
            set
            {
                _strokeDashStyle = value;
                RaisePropertyChanged("StrokeDashStyle");
                Drawme();
            }
        }

        [DataMember(Name = "cr", EmitDefaultValue = false)]
        public double CornersRoundness
        {
            get { return _cornersRoundness; }
            set
            {
                _cornersRoundness = value;
                RaisePropertyChanged("CornersRoundness");
                Drawme();
            }
        }

        #endregion Stroke

        #region Font

        [DataMember(Name = "fn", EmitDefaultValue = false)]
        public string FontName
        {
            get { return _fontName; }
            set
            {
                _fontName = value;
                RaisePropertyChanged("FontName");
                if (IsControl && _control != null)
                    Control.FontFamily = new FontFamily(_fontName);
                Drawme();
            }
        }

        [DataMember(Name = "fs", EmitDefaultValue = false)]
        public double FontSize
        {
            get { return _fontSize; }
            set
            {
                _fontSize = value;
                RaisePropertyChanged("FontSize");
                if (IsControl && _control != null)
                    Control.FontSize = _fontSize;
                Drawme();
            }
        }

        [DataMember(Name = "fb", EmitDefaultValue = false)]
        public bool FontIsBold
        {
            get { return _fontIsBold; }
            set
            {
                _fontIsBold = value;
                RaisePropertyChanged("FontIsBold");
                if (IsControl && _control != null)
                    Control.FontWeight = _fontIsBold ? FontWeights.Bold : FontWeights.Normal;
                Drawme();
            }
        }

        [DataMember(Name = "fi", EmitDefaultValue = false)]
        public bool FontIsItalic
        {
            get { return _fontIsItalic; }
            set
            {
                _fontIsItalic = value;
                RaisePropertyChanged("FontIsItalic");
                if (IsControl && _control != null)
                    Control.FontStyle = _fontIsItalic ? FontStyles.Italic : FontStyles.Normal;
                Drawme();
            }
        }

        [DataMember(Name = "ta", EmitDefaultValue = false)]
        public FigHorizontalAlignment TextAlignment
        {
            get { return _textAlignment; }
            set
            {
                _textAlignment = value;
                RaisePropertyChanged("TextAlignment");
                if (!IsTextContent || _control == null) return;
                switch (Kind)
                {
                    case FigKind.Hyperlink:
                        ((TextBlock)((Label)Control).Content).TextAlignment = (TextAlignment)TextAlignment;
                        break;
                    case FigKind.Textbox:
                        ((TextBox)Control).TextAlignment = (TextAlignment)TextAlignment;
                        break;
                    case FigKind.Button:
                        ((TextBlock)((Button)Control).Content).TextAlignment = (TextAlignment)TextAlignment;
                        break;
                }
                switch (TextAlignment)
                {
                    case FigHorizontalAlignment.Left:
                        Control.HorizontalContentAlignment = HorizontalAlignment.Left;
                        break;
                    case FigHorizontalAlignment.Right:
                        Control.HorizontalContentAlignment = HorizontalAlignment.Right;
                        break;
                    case FigHorizontalAlignment.Center:
                        Control.HorizontalContentAlignment = HorizontalAlignment.Center;
                        break;
                    case FigHorizontalAlignment.Stretch:
                        Control.HorizontalContentAlignment = HorizontalAlignment.Stretch;
                        break;
                }
                Drawme();
            }
        }

        [DataMember(Name = "tc", EmitDefaultValue = false)]
        public Int32 IntFontColor { get; set; }

        // ReSharper disable MemberCanBePrivate.Global
        public Color FontColor
        // ReSharper restore MemberCanBePrivate.Global
        {
            get
            {
                var arr = BitConverter.GetBytes(IntFontColor);
                return Color.FromArgb(arr[3], arr[0], arr[1], arr[2]);
            }
            set
            {
                var arr = new[] { value.R, value.G, value.B, value.A };
                IntFontColor = BitConverter.ToInt32(arr, 0);
                RaisePropertyChanged("FontColor");
                if (IsControl && _control != null)
                {
                    var btn = Control as Button;
                    if (btn != null)
                        btn.Foreground = FontBrush;
                    else
                        Control.Foreground = FontBrush;
                }
                Drawme();
            }
        }

        private Brush FontBrush
        {
            get
            {
                return new SolidColorBrush(FontColor);
            }
        }

        [DataMember(Name = "tcb", EmitDefaultValue = false)]
        public bool FontColorBlink
        {
            get { return _fontColorBlink; }
            set
            {
                _fontColorBlink = value;
                RaisePropertyChanged("FontColorBlink");
                Drawme();
            }
        }

        #endregion

        [DataMember(Name = "k", EmitDefaultValue = false)]
        public FigKind Kind { get; set; }

        [DataMember(Name = "t", EmitDefaultValue = false)]
        public string Text
        {
            get { return _text; }
            set
            {
                _text = value; 
                RaisePropertyChanged("Text");
                if (!IsTextContent || _control == null) return;
                switch (Kind)
                {
                    case FigKind.Checkbox:
                        ((CheckBox) Control).Content = _text;
                        break;
                    case FigKind.Textbox:
                        ((TextBox) Control).Text = _text;
                        break;
                    case FigKind.Hyperlink:
                        var tbox = ((TextBlock)((Label) Control).Content);
                        tbox.Inlines.Clear();
                        tbox.Inlines.Add(new Hyperlink(new Run(_text)));
                        break;
                    case FigKind.Button:
                        ((TextBlock)((Button) Control).Content).Text = _text;
                        break;
                    case FigKind.Combobox:
                        ((ComboBox) Control).Text = _text;
                        break;
                }
            }
        }

        private double _rotation;

        [DataMember(Name = "r", EmitDefaultValue = false)]
        public double Rotation
        {
            get { return _rotation; }
            set { _rotation = value; RaisePropertyChanged("Rotation"); }
        }

        [DataMember(Name = "v", EmitDefaultValue = false)]
        public FigVisibility Visibility
        {
            get { return _visibility; }
            set { _visibility = value; RaisePropertyChanged("Visibility"); }
        }

        [DataMember(Name = "ten", EmitDefaultValue = false)]
        public bool TabEnabled
        {
            get { return _tabEnabled; }
            set { _tabEnabled = value; RaisePropertyChanged("TabEnabled"); }
        }

        [DataMember(Name = "tidx", EmitDefaultValue = false)]
        public int TabIndex
        {
            get { return _tabIndex; }
            set { _tabIndex = value; RaisePropertyChanged("TabIndex"); }
        }

        [DataMember(Name = "fpo", EmitDefaultValue = false)]
        public bool FaceplateOption
        {
            get { return _faceplateOption; }
            set { _faceplateOption = value; RaisePropertyChanged("FaceplateOption"); }
        }

        [DataMember(Name = "hvo", EmitDefaultValue = false)]
        public bool HoverOption
        {
            get { return _hoverOption; }
            set { _hoverOption = value; RaisePropertyChanged("HoverOption"); }
        }

        [DataMember(Name = "ppo", EmitDefaultValue = false)]
        public bool PopupOption
        {
            get { return _popupOption; }
            set { _popupOption = value; RaisePropertyChanged("PopupOption"); }
        }

        [DataMember(Name = "sdo", EmitDefaultValue = false)]
        public bool ScriptdataOption
        {
            get { return _scriptdataOption; }
            set { _scriptdataOption = value; RaisePropertyChanged("ScriptdataOption"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private DrawingVisual _visual;

        public DrawingVisual Visual
        {
            get
            {
                _visual = _visual ?? new DrawingVisual();
                return _visual;
            }
        }

        private Control _control;

        public void SetControlSize(double? width, double? height)
        {
            if (_control == null) return;
            if (width != null) _control.Width = (double)width >= MinWidth ? (double)width : MinWidth;
            if (height != null) _control.Height = (double)height >= MinHeight ? (double)height : MinHeight;
        }

        public bool CreateControl(bool restored = false)
        {
            switch (Kind)
            {
                case FigKind.Textbox:
                    Text = Text ?? "TextBox";
                    _control = new TextBox
                        {
                            Text = Text,
                            FontFamily = new FontFamily(FontName),
                            FontSize = FontSize,
                            FontStyle = FontIsItalic ? FontStyles.Italic : FontStyles.Normal,
                            FontWeight = FontIsBold ? FontWeights.Bold : FontWeights.Normal,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = (TextAlignment)TextAlignment,
                            Width = Width,
                            Height = Height
                        };
                    if (restored)
                    {
                        if (FillMode != FillMode.None)
                            _control.Background = FillBrush;
                        _control.Foreground = FontBrush;
                    }
                    break;
                case FigKind.Hyperlink:
                    Text = Text ?? "HyperLink";
                    _control = new Label
                        {
                            FontFamily = new FontFamily(FontName),
                            FontSize = FontSize,
                            FontStyle = FontIsItalic ? FontStyles.Italic : FontStyles.Normal,
                            FontWeight = FontIsBold ? FontWeights.Bold : FontWeights.Normal,
                            Width = Width,
                            Height = Height
                        };
                    var content = new TextBlock
                        {
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = (TextAlignment)TextAlignment
                        };
                    content.Inlines.Add(new Hyperlink(new Run(Text)));
                    ((Label) _control).Content = content;
                    if (restored)
                    {
                        if (FillMode != FillMode.None)
                            _control.Background = FillBrush;
                        _control.Foreground = FontBrush;
                    }
                    break;
                case FigKind.Checkbox:
                    Text = Text ?? "CheckBox";
                    _control = new CheckBox
                        {
                            Content = Text,
                            FontFamily = new FontFamily(FontName),
                            FontSize = FontSize,
                            FontStyle = FontIsItalic ? FontStyles.Italic : FontStyles.Normal,
                            FontWeight = FontIsBold ? FontWeights.Bold : FontWeights.Normal,
                            Width = Width,
                            Height = Height
                        };
                    if (restored)
                    {
                        if (FillMode != FillMode.None)
                            _control.Background = FillBrush;
                        _control.Foreground = FontBrush;
                    }
                    break;
                case FigKind.Button:
                    Text = Text ?? "Button";
                    _control = new Button
                        {
                            Content = new TextBlock
                            {
                                Text = Text,
                                TextWrapping = TextWrapping.Wrap,
                                TextAlignment = (TextAlignment)TextAlignment
                            },
                            FontFamily = new FontFamily(FontName),
                            FontSize = FontSize,
                            FontStyle = FontIsItalic ? FontStyles.Italic : FontStyles.Normal,
                            FontWeight = FontIsBold ? FontWeights.Bold : FontWeights.Normal,
                            Width = Width,
                            Height = Height
                        };
                    if (restored)
                    {
                        if (FillMode != FillMode.None)
                            _control.Background = FillBrush;
                        _control.Foreground = FontBrush;
                    }
                    break;
                case FigKind.Combobox:
                    Text = Text ?? "ComboBox";
                    _control = new ComboBox
                        {
                            Text = Text,
                            FontFamily = new FontFamily(FontName),
                            FontSize = FontSize,
                            FontStyle = FontIsItalic ? FontStyles.Italic : FontStyles.Normal,
                            FontWeight = FontIsBold ? FontWeights.Bold : FontWeights.Normal,
                            Width = Width,
                            Height = Height
                        };
                    if (restored)
                    {
                        if (FillMode != FillMode.None)
                            _control.Background = FillBrush;
                        _control.Foreground = FontBrush;
                    }
                    break;
                case FigKind.Shapelink:
                    _control = new DrawingPage
                        {
                            Transparent = true,
                            Width = Width,
                            Height = Height
                        };
                    break;
                default:
                    return false;
            }
            return true;
        }

        public Control Control
        {
            get
            {
                if (IsControl && _control == null) 
                    throw new NotImplementedException();
                return _control;
            }
        }

        public bool IsStrokeContent
        {
            get
            {
                switch (Kind)
                {
                    case FigKind.Rectangle:
                    case FigKind.Circle:
                    case FigKind.Label:
                        return true;
                }
                return false;
            }
        }

        public bool IsRectCorners
        {
            get
            {
                switch (Kind)
                {
                    case FigKind.Rectangle:
                    case FigKind.Label:
                        return true;
                }
                return false;
            }
        }


        public bool IsTextContent
        {
            get
            {
                switch (Kind)
                {
                    case FigKind.Label:
                    case FigKind.Textbox:
                    case FigKind.Hyperlink:
                    case FigKind.Checkbox:
                    case FigKind.Button:
                    case FigKind.Combobox:
                        return true;
                }
                return false;
            }
        }

        public bool IsCommonText
        {
            get
            {
                return Kind == FigKind.Label || Kind == FigKind.Hyperlink || Kind == FigKind.Textbox;
            }
        }

        public bool IsButton { get { return Kind == FigKind.Button; } }

        public bool IsCheckbox { get { return Kind == FigKind.Checkbox; } }

        public bool IsNotControl { get { return !IsControl; } }

        public bool IsControl
        {
            get
            {
                switch (Kind)
                {
                    case FigKind.Textbox:
                    case FigKind.Hyperlink:
                    case FigKind.Checkbox:
                    case FigKind.Button:
                    case FigKind.Combobox:
                    case FigKind.Shapelink:
                        return true;
                }
                return false;
            }
            
        }

        public Fig()
        {
            Init();
        }

        private void Init()
        {
            FillColor = Colors.White;
            FillGradientColor = Colors.Gray;
            FillMode = FillMode.Solid;
            StrokeThickness = 1.0;
            StrokeColor = Colors.Black;
            StrokeDashStyle = FigDashStyles.Solid;
            FontColor = Colors.Black;
            FontName = "Arial";
            FontSize = 12;
            if (Math.Abs(Width - 0) < 0.0001) Width = 30;
            if (Math.Abs(Height - 0) < 0.0001) Height = 30;
        }

        public Fig(FigKind kind) : this()
        {
            Kind = kind;
            if (IsControl)
                _fillMode = FillMode.None;
            if (kind == FigKind.Label)
                Text = "Label";
            CreateControl();
       }

        public void SetOrderLocation(int index)
        {
            Index = index;
        }

        public Vector ClickOffset { get; set; }

        private bool _shapeLoaded;

        private byte[] _imageData;
        private double _left;
        private double _top;
        private FigVisibility _visibility = FigVisibility.Inherit;
        private bool _tabEnabled;
        private int _tabIndex;
        private bool _faceplateOption;
        private bool _popupOption;
        private bool _scriptdataOption;
        private bool _hoverOption;
        private FillMode _fillMode;
        private GradientMode _gradientMode;
        private double _fillOffset;
        private double _fillAngle;
        private bool _fillColorBlink;
        private bool _strokeColorBlink;
        private bool _fontColorBlink;
        private string _fontName = "Arial";
        private double _fontSize = 12;
        private double _strokeThickness = 1.0;
        private FigDashStyles _strokeDashStyle = FigDashStyles.Solid;
        private bool _fontIsBold;
        private bool _fontIsItalic;
        private FigHorizontalAlignment _textAlignment;
        private string _text;
        private double _cornersRoundness;

        public void Drawme(bool? blink = null)
        {
            if (blink != null && IsControl) return;
            var topLeftCorner = new Point(Left, Top);
            Drawme(topLeftCorner, blink);
        }

        public void Drawme(Point topLeftCorner, bool? blink = null)
        {
            using (var dc = Visual.RenderOpen())
            {
                if (Group == null)
                    Drawme(dc, topLeftCorner, blink);
                else
                {
                    foreach (var child in Group)
                    {
                        child.Drawme(dc,
                            new Point(topLeftCorner.X +child.Left, topLeftCorner.Y +child.Top), blink);
                    }
                }
            }
            Left = topLeftCorner.X;
            Top = topLeftCorner.Y;
        }

        private void Drawme(DrawingContext dc, Point topLeftCorner, bool? blink = null)
        {
            if (Group == null)
                Drawself(dc, topLeftCorner, blink);
            else
                foreach (var child in Group)
                    child.Drawme(dc,
                        new Point(topLeftCorner.X + child.Left, topLeftCorner.Y + child.Top), blink);
        }

        private void Drawself(DrawingContext dc, Point topLeftCorner, bool? blink = null)
        {
            Pen pen;
            VisualBrush brush;
            string filename;
            var fillBrush = FillBrush;
            if (FillColorBlink && blink == true)
                fillBrush = Brushes.Transparent;
            var strokePen = StrokePen;
            if (StrokeColorBlink && blink == true || StrokeDashStyle == FigDashStyles.Noline)
                strokePen = new Pen(Brushes.Transparent, StrokeThickness);
            switch (Kind)
            {
                case FigKind.Rectangle:
                    dc.DrawRoundedRectangle(fillBrush, strokePen, 
                        new Rect(topLeftCorner, new Size(Width, Height)), 
                        (Math.Min(Width, Height) * 0.5 * CornersRoundness),
                         Math.Min(Width, Height) * 0.5 * CornersRoundness);
                    break;
                case FigKind.Circle:
                    var center = topLeftCorner;
                    center.Offset(Width * 0.5, Height * 0.5);
                    dc.DrawEllipse(fillBrush, strokePen, center, Width * 0.5, Height * 0.5);
                    break;
                case FigKind.Label:
                    dc.DrawRoundedRectangle(fillBrush, strokePen, 
                        new Rect(topLeftCorner, new Size(Width, Height)), 
                        (Math.Min(Width, Height) * 0.5 * CornersRoundness),
                         Math.Min(Width, Height) * 0.5 * CornersRoundness);
                    var tf = new Typeface(new FontFamily(FontName),
                                          FontIsItalic ? FontStyles.Italic : FontStyles.Normal, 
                                          FontIsBold ? FontWeights.Bold : FontWeights.Normal, 
                                          FontStretches.Normal);
                    var fontBrush = FontBrush;
                    if (FontColorBlink && blink == true)
                        fontBrush = Brushes.Transparent;
                    var ft = new FormattedText(Text ?? "", CultureInfo.GetCultureInfo("ru-RU"),
                                               FlowDirection.LeftToRight,
                                               tf, FontSize, fontBrush)
                        {
                            TextAlignment = (TextAlignment) TextAlignment,
                            MaxTextWidth = Math.Max(Width - StrokeThickness, 1),
                            MaxTextHeight = Math.Max(Height - StrokeThickness, 1),
                            Trimming = TextTrimming.None
                        };
                    var step = Height / 20;
                    for (var i = Height - step; i >= step; i -= step)
                    {
                        if (ft.Height <= Height) break;
                        ft.SetFontSize(i);
                    }
                    topLeftCorner.X += StrokeThickness * 0.5;
                    topLeftCorner.Y += (Height - ft.Height) / 2;
                    dc.DrawText(ft, topLeftCorner);
                    break;
                case FigKind.Textbox:
                case FigKind.Hyperlink:
                case FigKind.Checkbox:
                case FigKind.Button:
                case FigKind.Combobox:
                    if (_control != null)
                    {
                        pen = new Pen(Brushes.Transparent, StrokeThickness);
                        brush = new VisualBrush(Control);
                        var height = Control.Height;
                        if (double.IsNaN(height)) height = Height;
                        var width = Control.Width;
                        if (double.IsNaN(width)) width = Width;
                        dc.DrawRectangle(brush, pen, new Rect(topLeftCorner, new Size(width, height)));
                    }
                    else
                    {
                        dc.DrawRectangle(Brushes.Transparent, new Pen(Brushes.Black, StrokeThickness) { DashStyle = DashStyles.Dot}, 
                            new Rect(topLeftCorner, new Size(Width, Height)));
                    }
                    break;
                case FigKind.Shapelink:
                    filename = Text ?? "";
                    if (GetFilesFolder != null)
                    {
                        var folder = GetFilesFolder();
                        var shape = (DrawingPage) Control;
                        var shapefile = Path.Combine(folder, filename);
                        if (!_shapeLoaded && File.Exists(shapefile))
                        {
                            shape.LoadShape(shapefile);
                            Control.Width = Width;
                            Control.Height = Height;
                            shape.Zoom(ZoomMode.Scale100);
                            shape.InvalidateVisual();
                            _shapeLoaded = true;

                            pen = new Pen(Brushes.Transparent, StrokeThickness); 
                            brush = new VisualBrush(Control);
                            dc.DrawRectangle(brush, pen, new Rect(topLeftCorner, new Size(Control.Width, Control.Height)));
                        }
                    }
                    break;
                case FigKind.Imagelink:
                    pen = new Pen(Brushes.Transparent, StrokeThickness); 
                    dc.DrawRectangle(Brushes.Transparent, pen, new Rect(topLeftCorner, new Size(Width, Height)));
                    filename = Text ?? "";
                    if (GetFilesFolder != null)
                    {
                        var folder = GetFilesFolder();
                        var imagefile = Path.Combine(folder, filename);
                        if (_imageData == null)
                        {
                            if (File.Exists(imagefile))
                            _imageData = File.ReadAllBytes(imagefile);
                        }
                        if (_imageData != null)
                        {
                            var image = new BitmapImage();
                            using (var mem = new MemoryStream(_imageData))
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
                            dc.DrawImage(image, new Rect(topLeftCorner, new Size(Width, Height)));
                        }
                    }
                    break;
            }
        }
    }
}

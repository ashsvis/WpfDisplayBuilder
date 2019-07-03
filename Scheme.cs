using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.Serialization;
using WpfDisplayBuilder.Properties;

namespace WpfDisplayBuilder
{
    public enum SecurityLevel
    {
        [LocalizableDescription(@"Any", typeof(Resources))]
        Any,
        [LocalizableDescription(@"Oper", typeof(Resources))]
        Oper,
        [LocalizableDescription(@"Supr", typeof(Resources))]
        Supr,
        [LocalizableDescription(@"Engr", typeof(Resources))]
        Engr,
        [LocalizableDescription(@"Mngr", typeof(Resources))]
        Mngr
    }

    [DataContract]
    public class Scheme : FilterablePropertyBase, INotifyPropertyChanged
    {
        private bool _allAssets = true;
        private double _width;
        private double _height;
        private string _backgroundImageUri;
        private string _name;
        private string _descriptor;
        private string _stylesheet;
        private uint _refreshPageTime;
        private bool _useZoom;
        private SecurityLevel _securityLevel;
        private bool _onlyAssets;
        private string _assets;
        private Fig[] _items;
        private CustomProperty[] _customs;

        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged("Name"); }
        }

        [DataMember(Name = "desc", EmitDefaultValue = false)]
        public string Descriptor
        {
            get { return _descriptor; }
            set { _descriptor = value; RaisePropertyChanged("Descriptor"); }
        }

        [DataMember(Name = "ss", EmitDefaultValue = false)]
        public string Stylesheet
        {
            get { return _stylesheet; }
            set { _stylesheet = value; RaisePropertyChanged("Stylesheet"); }
        }

        [DataMember(Name = "rpt", EmitDefaultValue = false)]
        public uint RefreshPageTime
        {
            get { return _refreshPageTime; }
            set { _refreshPageTime = value; RaisePropertyChanged("RefreshPageTime"); }
        }

        [DataMember(Name = "uz", EmitDefaultValue = false)]
        public bool UseZoom
        {
            get { return _useZoom; }
            set { _useZoom = value; RaisePropertyChanged("UseZoom"); }
        }

        [DataMember(Name = "sl", EmitDefaultValue = false)]
        public SecurityLevel SecurityLevel
        {
            get { return _securityLevel; }
            set { _securityLevel = value; RaisePropertyChanged("SecurityLevel"); }
        }

        [DataMember(Name = "aa", EmitDefaultValue = false), DefaultValue(true)]
        public bool AllAssets { get { return _allAssets; } set { _allAssets = value; RaisePropertyChanged("AllAssets"); } }

        [DataMember(Name = "oa", EmitDefaultValue = false)]
        public bool OnlyAssets
        {
            get { return _onlyAssets; }
            set { _onlyAssets = value; RaisePropertyChanged("OnlyAssets"); }
        }

        [DataMember(Name = "as", EmitDefaultValue = false)]
        public string Assets
        {
            get { return _assets; }
            set { _assets = value; RaisePropertyChanged("Assets"); }
        }

        [DataMember(Name = "items", EmitDefaultValue = false)]
        public Fig[] Items
        {
            get { return _items; }
            set { _items = value; RaisePropertyChanged("Items"); }
        }

        [DataMember(Name = "custs", EmitDefaultValue = false)]
        public CustomProperty[] Customs
        {
            get { return _customs; }
            set { _customs = value; RaisePropertyChanged("Customs"); }
        }

        [DataMember(Name = "w", EmitDefaultValue = false)]
        public double Width { get { return _width; } set { _width = value; RaisePropertyChanged("Width"); } }

        [DataMember(Name = "h", EmitDefaultValue = false)]
        public double Height { get { return _height; } set { _height = value; RaisePropertyChanged("Height"); } }

        [DataMember(Name = "biu", EmitDefaultValue = false)]
        public string BackgroundImageUri
        {
            get { return _backgroundImageUri; }
            set { _backgroundImageUri = value; RaisePropertyChanged("BackgroundImageUri"); }
        }

        [DataMember(Name = "fc", EmitDefaultValue = false), DefaultValue(-1)]
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
            }
        }

        public bool IsSaved { get; set; }

        public Scheme()
        {
            FillColor = Colors.Silver;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //Console.WriteLine("Scheme: " + propertyName);
        }
    }
}
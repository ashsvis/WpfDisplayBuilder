using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows.Media;

namespace WpfDisplayBuilder
{
    [DataContract]
    public class Shape : FilterablePropertyBase, INotifyPropertyChanged
    {
        private double _width;
        private double _height;
        private string _name;
        private string _descriptor;
        private string _stylesheet;
        private bool _firstAsBad;
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

        [DataMember(Name = "fab", EmitDefaultValue = false)]
        public bool FirstAsBad
        {
            get { return _firstAsBad; }
            set { _firstAsBad = value; RaisePropertyChanged("FirstAsBad"); }
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

        [DataMember(Name = "l", EmitDefaultValue = false)]
        public double Left { get; set; }

        [DataMember(Name = "t", EmitDefaultValue = false)]
        public double Top { get; set; }

        [DataMember(Name = "w", EmitDefaultValue = false)]
        public double Width { get { return _width; } set { _width = value; RaisePropertyChanged("Width"); } }

        [DataMember(Name = "h", EmitDefaultValue = false)]
        public double Height { get { return _height; } set { _height = value; RaisePropertyChanged("Height"); } }

        public string BackgroundImageUri
        {
            get { return ""; }
            set {  }
        }

        public Color FillColor
        {
            get
            {
                return Colors.Silver;
            }
            set
            {
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            //Console.WriteLine("Shape: " + propertyName);
        }
    }
}
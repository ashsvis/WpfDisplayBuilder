using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplayBuilder.Properties;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Логика взаимодействия для FigPropsEditor.xaml
    /// </summary>
    public partial class FigPropsEditor : Window
    {
        private readonly ObservableCollection<CustomProperty> _customProps =
            new ObservableCollection<CustomProperty>();

        private Fig _selfig;
        private IEnumerable<Fig> _sellist;

        public FigPropsEditor(Fig selfig, IEnumerable<Fig> sellist)
        {
            InitializeComponent();
            UpdateOnSelect(selfig, sellist);
            CustomsTable.ItemsSource = _customProps;
            CustomsTable.CellEditEnding += CustomsTable_CellEditEnding;
            _customProps.CollectionChanged += _customProps_CollectionChanged; 
        }

        public static IEnumerable<string> SimpleColorList
        {
            get
            {
                return typeof(Colors).GetProperties().Select(pi => pi.Name).ToList();
            }
        }

        public static IEnumerable<string> SimpleFontList
        {
            get
            {
                //создаём список для шрифтов
                var listFontFamiliyes = new List<FontFamily>();
                //заполняем список шрифтами
                listFontFamiliyes.AddRange(Fonts.GetFontFamilies(Environment.GetFolderPath(Environment.SpecialFolder.Fonts)));
                //создаём список для хранения названий шрифта
                var listFontNames = new List<String>();
                foreach (var fontFamily in listFontFamiliyes)
                {
                    var str = new List<String>();
                    if (fontFamily.FamilyNames.Values != null) 
                        str.AddRange(fontFamily.FamilyNames.Values);
                    listFontNames.AddRange(str);
                }
                listFontNames.Sort();
                return listFontNames;
            }
        }

        public static IEnumerable<string> SimpleFontSizes 
        {  
            get
            {
                var listFontSizes = new List<String> 
                { "6", "7", "8", "9", "10", "11", "12", "14", "16", 
                  "18", "20", "22", "24", "26", "28", "36", "48", "72" };
                return listFontSizes;
            } 
        }

        public static IEnumerable<string> SimpleThicknessList
        {
            get
            {
                var listThickness = new List<String> 
                { "1", "2", "3", "4", "5", "6", "7", "8" };
                return listThickness;
            }
        }

        void CustomsTable_CellEditEnding(object sender, 
            DataGridCellEditEndingEventArgs e)
        {
            UpdateCustomsArray();
        }

        void _customProps_CollectionChanged(object sender, 
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateCustomsArray();
        }

        public void UpdateOnSelect(Fig selfig, IEnumerable<Fig> sellist)
        {
            _selfig = selfig;
            _sellist = sellist;
            DataContext = _selfig;
            if (_selfig != null)
            {
                var fp = CultureInfo.GetCultureInfo("en-US");
                LeftEdit.Text = selfig.Left.ToString(fp);
                TopEdit.Text = selfig.Top.ToString(fp);
                WidthEdit.Text = selfig.Width.ToString(fp);
                HeightEdit.Text = selfig.Height.ToString(fp);
            }
            CustomsTable.CellEditEnding -= CustomsTable_CellEditEnding;
            _customProps.CollectionChanged -= _customProps_CollectionChanged;
            _customProps.Clear();
            if (_selfig != null &&_selfig.Customs != null)
                foreach (var prop in _selfig.Customs)
                    _customProps.Add(prop);
            CustomsTable.CellEditEnding += CustomsTable_CellEditEnding;
            _customProps.CollectionChanged += _customProps_CollectionChanged;
        }

        private void UpdateCustomsArray()
        {
            if (_selfig == null) return;
            if (_selfig.Customs == null && _customProps.Count == 0) return;
            _selfig.Customs = _customProps.Count > 0 
                ? (new List<CustomProperty>(_customProps)).ToArray() 
                : null;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            _customProps.Add(new CustomProperty());
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var cp = CustomsTable.SelectedValue as CustomProperty;
            if (cp != null)
                _customProps.Remove(cp);
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null || e.Key != Key.Enter) return;
            var be = tbx.GetBindingExpression(TextBox.TextProperty);
            if (be != null)
                UpdateSelectedSource(be, tbx.Tag + "", true);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null) return;
            var be = tbx.GetBindingExpression(TextBox.TextProperty);
            if (be != null && tbx.Tag != null && (bool) tbx.Tag)
                UpdateSelectedSource(be, tbx.Tag + "");
        }

        private void UpdateSelectedSource(BindingExpressionBase be, string propName, bool selectAll = false)
        {
            be.UpdateSource();
            if (selectAll)
                switch (propName)
                {
                    case "Left":
                        LeftEdit.SelectAll();
                        break;
                    case "Top":
                        TopEdit.SelectAll();
                        break;
                    case "Width":
                        WidthEdit.SelectAll();
                        break;
                    case "Height":
                        HeightEdit.SelectAll();
                        break;
                }
            _selfig.Parent.UpdateSelected();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null) return;
            tbx.Tag = true;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null) return;
            tbx.Tag = null;
        }

        private void FillColorButton_Click(object sender, RoutedEventArgs e)
        {
            FillColorBorder.Visibility = Visibility.Visible;
            FillGradientColorBorder.Visibility = Visibility.Collapsed;
            StrokeColorBorder.Visibility = Visibility.Collapsed;
            FontColorBorder.Visibility = Visibility.Collapsed;
        }

        private void FillGradientColorButton_Click(object sender, RoutedEventArgs e)
        {
            FillColorBorder.Visibility = Visibility.Collapsed;
            FillGradientColorBorder.Visibility = Visibility.Visible;
            StrokeColorBorder.Visibility = Visibility.Collapsed;
            FontColorBorder.Visibility = Visibility.Collapsed;
        }

        private void StrokeColorButton_Click(object sender, RoutedEventArgs e)
        {
            FillColorBorder.Visibility = Visibility.Collapsed;
            FillGradientColorBorder.Visibility = Visibility.Collapsed;
            StrokeColorBorder.Visibility = Visibility.Visible;
            FontColorBorder.Visibility = Visibility.Collapsed;
        }

        private void TextColorButton_Click(object sender, RoutedEventArgs e)
        {
            FillColorBorder.Visibility = Visibility.Collapsed;
            FillGradientColorBorder.Visibility = Visibility.Collapsed;
            StrokeColorBorder.Visibility = Visibility.Collapsed;
            FontColorBorder.Visibility = Visibility.Visible;
        }

        private void BrushKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;
            switch ((FillMode)e.AddedItems[0])
            {
                case FillMode.Solid:
                case FillMode.None:
                    FillGradientColorButton.Visibility = Visibility.Collapsed;
                    break;
                case FillMode.Gradient:
                    FillGradientColorButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        private void GradientKind_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0) return;
            switch ((GradientMode)e.AddedItems[0])
            {
                case GradientMode.LinearGradient:
                    FillOffsetPanel.Visibility = Visibility.Visible;
                    break;
                case GradientMode.RadialGradient:
                    FillOffsetPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void FillGradientColorButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool) e.NewValue == false)
                FillGradientColorBorder.Visibility = Visibility.Collapsed;
        }

        private void TextColorButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
                FontColorBorder.Visibility = Visibility.Collapsed;
        }

        private void StrokeColorButton_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
                StrokeColorBorder.Visibility = Visibility.Collapsed;
        }

        private void TabItem_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PropsTabControl.SelectedIndex = 0;
        }
    }

    public enum FillMode : int
    {
        [LocalizableDescription(@"SolidBrush", typeof(Resources))]
        Solid = 0,
        [LocalizableDescription(@"GradientBrush", typeof(Resources))]
        Gradient = 1,
        [LocalizableDescription(@"NoBrush", typeof(Resources))]
        None = 2
    }

    public enum GradientMode : int
    {
        [LocalizableDescription(@"LinearGradient", typeof(Resources))]
        LinearGradient = 0,
        [LocalizableDescription(@"RadialGradient", typeof(Resources))]
        RadialGradient = 1
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class ColorConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (from item in typeof (Colors).GetProperties() 
                    where value.ToString().Equals(item.GetValue(item, null).ToString()) 
                    select item.Name).FirstOrDefault();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (
                var brush in
                    from item in typeof (Colors).GetProperties()
                    where item.Name == value.ToString()
                    select (Color) item.GetValue(item, null))
                return brush;
            return null;
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    public class StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            if (targetType == typeof (double))
                return ((double)value).ToString(culture);
            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double dv;
            if (targetType == typeof (double) && double.TryParse(value.ToString(), NumberStyles.Any, culture, out dv))
                return dv;
            throw new NotImplementedException();
        }
    }

}

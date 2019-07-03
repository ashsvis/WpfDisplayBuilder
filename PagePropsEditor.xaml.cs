using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Логика взаимодействия для PagePropsEditor.xaml
    /// </summary>
    public partial class PagePropsEditor : Window
    {
        private readonly Scheme _scheme;
        private readonly string _localFolder;

        private readonly ObservableCollection<CustomProperty> _customProps =
            new ObservableCollection<CustomProperty>();

        public static IEnumerable<string> SimpleColorList
        {
            get
            {
                return typeof(Colors).GetProperties().Select(pi => pi.Name).ToList();
            }
        }

        public PagePropsEditor(Scheme scheme, string localFolder)
        {
            InitializeComponent();
            _scheme = scheme;
            _localFolder = localFolder;
            DataContext = _scheme;
            _customProps.Clear();
            if (_scheme.Customs != null)
                foreach (var prop in _scheme.Customs)
                    _customProps.Add(prop);
            CustomsTable.ItemsSource = _customProps;
            CustomsTable.CellEditEnding += (o, e) => UpdateCustomsArray();
            _customProps.CollectionChanged += (o, e) => UpdateCustomsArray();
        }

        private void UpdateCustomsArray()
        {
            _scheme.Customs = _customProps.Count > 0 ? (new List<CustomProperty>(_customProps)).ToArray() : null;
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

        private void StyleUriButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".css",
                Filter = "Файлы CSS (*.css)|*.css|Все файлы (*.*)|*.*"
            };
            // Show open file dialog box
            var result = dlg.ShowDialog();
            // Process open file dialog box results
            if (result != true) return;
            // Open document
            StyleUri.Text = dlg.FileName;
        }

        private string _imagefolder;

        private void BrowseBackgroundImageUriButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var folder = _imagefolder ?? Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures);
            var dlg = new OpenFileDialog
            {
                InitialDirectory = folder,
                FileName = "",
                DefaultExt = ".png",
                Filter = "Файлы картинок (.png,.jpg,.bmp,.emf,.wmf,.gif)|*.png;*.jpg;*.bmp;*.emf;*.wmf;*.gif"
            };
            // Show open file dialog box
            var result = dlg.ShowDialog();
            // Process open file dialog box results
            if (result != true) return ;
            _imagefolder = Path.GetDirectoryName(dlg.FileName);
            // Open document
            var filename = Path.GetFileName(dlg.FileName) ?? "";
            if (String.IsNullOrWhiteSpace(filename)) return ;
            var destname = Path.Combine(_localFolder, filename);
            if (!File.Exists(destname))
                File.Copy(dlg.FileName, destname);
            BackgroundImageUri.Text = filename;
            var be = BackgroundImageUri.GetBindingExpression(TextBox.TextProperty);
            if (be != null) be.UpdateSource();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null || e.Key != Key.Enter) return;
            var be = tbx.GetBindingExpression(TextBox.TextProperty);
            if (be != null) be.UpdateSource();
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var tbx = sender as TextBox;
            if (tbx == null) return;
            var be = tbx.GetBindingExpression(TextBox.TextProperty);
            if (be != null && tbx.Tag != null && (bool)tbx.Tag)
                be.UpdateSource();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}

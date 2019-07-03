using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Логика взаимодействия для ShapePropsEditor.xaml
    /// </summary>
    public partial class ShapePropsEditor : Window
    {
        private readonly Shape _figure;

        private readonly ObservableCollection<CustomProperty> _customProps = 
            new ObservableCollection<CustomProperty>();

        public ShapePropsEditor(Shape figure)
        {
            InitializeComponent();
            _figure = figure;
            DataContext = _figure;
            _customProps.Clear();
            if (_figure.Customs != null)
                foreach (var prop in _figure.Customs)
                    _customProps.Add(prop);
            CustomsTable.ItemsSource = _customProps;
            CustomsTable.CellEditEnding += (o, e) => UpdateCustomsArray();
            _customProps.CollectionChanged += (o, e) => UpdateCustomsArray();
            //Closing += (o, e) => UpdateCustomsArray();
        }

        private void UpdateCustomsArray()
        {
            _figure.Customs = _customProps.Count > 0 ? (new List<CustomProperty>(_customProps)).ToArray() : null;
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
            var dlg = new Microsoft.Win32.OpenFileDialog
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
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
    }
}

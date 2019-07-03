using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Логика взаимодействия для PreviewSchemeWindow.xaml
    /// </summary>
    public partial class PreviewSchemeWindow : Window
    {
        public PreviewSchemeWindow()
        {
            InitializeComponent();
        }

        private readonly Timer _timer = new Timer(10) {AutoReset = false};

        private string _filename;

        public void LoadContent(string filename)
        {
            _filename = filename;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DrawingSurface.OnZoomIn += (o, args) =>
            {
                ZoomSelector.SelectedIndex--;
            };
            DrawingSurface.OnZoomOut += (o, args) =>
            {
                ZoomSelector.SelectedIndex++;
            };

            var arr = (ZoomMode[])Enum.GetValues(typeof(ZoomMode));
            ZoomSelector.Items.Clear();
            foreach (var item in arr) ZoomSelector.Items.Add(Fined.Value(item));
            ZoomSelector.Text = Fined.Value(ZoomMode.ZoomToFit);
            ZoomSelector.SelectionChanged += ZoomSelector_SelectionChanged;
            //-------------------------------------------       
            _timer.Elapsed += _timer_Elapsed;
            _timer.Enabled = true;

        }
        void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(() =>
                    {
                        DrawingSurface.Zoom(ZoomMode.ZoomToFit);
                        DrawingSurface.LoadContentRuntime(_filename);
                    }));
        }

        void ZoomSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawingSurface.Zoom((ZoomMode)ZoomSelector.SelectedIndex);
        }

        private void ZoomSelector_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WpfDisplayBuilder
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var folder = Path.Combine(appData, "Ikar", "RemX", "Abstract");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            DrawingSurface.HomeFolder = folder;
            //------------------------------------------------------------------
            DrawingSurface.ContentChanged += (o, args) =>
                {
                    SaveContentButton.IsEnabled = true;
                };
            DrawingSurface.SelectUpdated += (o, args) =>
                {
                    GroupingButton.IsEnabled = args.MoreOneSelected;
                    UngroupingButton.IsEnabled = args.HasGroups;
                    BringToFrontButton.IsEnabled = args.HasSelected;
                    SendToBackButton.IsEnabled = args.HasSelected;
                    BringToUpButton.IsEnabled = args.HasSelected && !args.MoreOneSelected;
                    SendToDownButton.IsEnabled = args.HasSelected && !args.MoreOneSelected;
                    LockButton.IsEnabled = args.HasSelected;
                    NodeeditButton.IsEnabled = args.HasSelected;
                    RotatorButton.IsEnabled = args.HasSelected;
                    AlignleftButton.IsEnabled = args.MoreOneSelected;
                    AligncenterButton.IsEnabled = args.MoreOneSelected;
                    AlignrightButton.IsEnabled = args.MoreOneSelected;
                    AligntopButton.IsEnabled = args.MoreOneSelected;
                    AlignmiddleButton.IsEnabled = args.MoreOneSelected;
                    AlignbottomButton.IsEnabled = args.MoreOneSelected;
                    SamewidthButton.IsEnabled = args.MoreOneSelected;
                    SameheightButton.IsEnabled = args.MoreOneSelected;
                    SamebothButton.IsEnabled = args.MoreOneSelected;
                    EvenlyhorButton.IsEnabled = args.MoreTwoSelected;
                    EvenlyverButton.IsEnabled = args.MoreTwoSelected;
                    UpdatePropsEditorWindow(args.Item, args.List, false);
                };
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
            ZoomSelector.Text = Fined.Value(ZoomMode.Scale100);
            ZoomSelector.SelectionChanged += ZoomSelector_SelectionChanged;
            ConnectPageContextMenu();
        }

        void ZoomSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DrawingSurface.Zoom((ZoomMode)ZoomSelector.SelectedIndex);
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            if (_propWindowEditor != null) _propWindowEditor.Close();
            _propWindowEditor = null;
            if (_propFigEditor != null) _propFigEditor.Close();
            _propFigEditor = null;
            // Configure open file dialog box
            var dlg = new Microsoft.Win32.OpenFileDialog
                {
                    InitialDirectory = DrawingSurface.HomeFolder,
                    FileName = Path.GetFileName(DrawingSurface.FileName),
                    DefaultExt = DrawingSurface.EditorMode == EditorMode.AsScheme ? ".scm" : ".shp",
                    Filter = "Файлы (.scm,.shp)|*.scm;*.shp|Мнемосхемы (.scm)|*.scm|Фигуры (.shp)|*.shp"
                };
            // Show open file dialog box
            var result = dlg.ShowDialog();
            // Process open file dialog box results
            if (result != true) return;
            // Open document
            DrawingSurface.LoadContent(dlg.FileName);
            SaveContentButton.IsEnabled = false;
            DrawingSurface.EditMode = EditMode.SelectMove;
            ArrowButton.IsChecked = true;
            ArrowButton.Focus();
            Title = (DrawingSurface.EditorMode == EditorMode.AsScheme ? "Редактор мнемосхем - " : "Редактор фигур - ") + 
                Path.GetFileName(DrawingSurface.FileName);
            PreviewContentButton.IsEnabled = DrawingSurface.EditorMode == EditorMode.AsScheme;
            DrawingSurface.InvalidateVisual();
            AddImage.IsEnabled = AddShape.IsEnabled = true;
            if (DrawingSurface.EditorMode == EditorMode.AsScheme)
                ConnectPageContextMenu();
            else
                ConnectShapeContextMenu();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.SelectMove; 
            ArrowButton.IsChecked = true;
            ArrowButton.Focus();
            if (String.IsNullOrWhiteSpace(DrawingSurface.FileName))
                SaveAs_Click(null, null);
            else
            {
                DrawingSurface.SaveContent();
                SaveContentButton.IsEnabled = false;                
            }
            AddImage.IsEnabled = AddShape.IsEnabled = true;
        }

        private void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                InitialDirectory = DrawingSurface.HomeFolder,
                FileName = Path.GetFileName(DrawingSurface.FileName),
                DefaultExt = DrawingSurface.EditorMode == EditorMode.AsScheme ? ".scm" : ".shp",
                Filter = DrawingSurface.EditorMode == EditorMode.AsScheme ? "Мнемосхемы (.scm)|*.scm" : "Фигуры (.shp)|*.shp"
            };

            // Show open file dialog box
            var result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result != true) return;
            // Save document
            DrawingSurface.SaveContent(dlg.FileName);
            SaveContentButton.IsEnabled = false;
            Title = (DrawingSurface.EditorMode == EditorMode.AsScheme ? "Редактор мнемосхем - " : "Редактор фигур - ") +
                Path.GetFileName(DrawingSurface.FileName);
        }

        private void ZoomSelector_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void GroupingButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.Grouping();
        }

        private void UngroupingButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.Ungrouping();
        }

        private void ArrowButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.SelectMove;
            ArrowButton.IsChecked = true;
        }

        private void AddRectButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddRectangle;
            AddRectButton.IsChecked = true;
        }

        private void AddCircleButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddCircle;
            AddCircleButton.IsChecked = true;
        }

        private void AddTextbox_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddTextbox;
            AddTextbox.IsChecked = true;
        }

        private void AddCheckbox_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddCheckbox;
            AddCheckbox.IsChecked = true;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddButton;
            AddButton.IsChecked = true;
        }

        private void AddCombobox_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddCombobox;
            AddCombobox.IsChecked = true;
        }

        private void BringToFrontButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.BringToFront();
        }

        private void SendToBackButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.SendToBack();
        }

        private void ZoominButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.ZoomIn;
            ZoominButton.IsChecked = true;
        }

        private void ZoomoutButton_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.ZoomOut;
            ZoomoutButton.IsChecked = true;
        }

        private void AddHyperlink_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddHyperlink;
            AddHyperlink.IsChecked = true;
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddImage;
            AddImage.IsChecked = true;
        }

        private void AddShape_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddShape;
            AddShape.IsChecked = true;
        }

        private void CreateSchemeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_propWindowEditor != null) _propWindowEditor.Close();
            _propWindowEditor = null;
            if (_propFigEditor != null) _propFigEditor.Close();
            _propFigEditor = null;
            DrawingSurface.CreateScheme();
            AddImage.IsEnabled = AddShape.IsEnabled = false;
            SaveContentButton.IsEnabled = true;
            DrawingSurface.EditMode = EditMode.SelectMove;
            ArrowButton.IsChecked = true;
            Title = "Редактор мнемосхем";
            ConnectPageContextMenu();
            PreviewContentButton.IsEnabled = true;
        }

        private void ConnectPageContextMenu()
        {
            DrawingSurface.ContextMenu = null;            
        }

        private void CreateShapeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_propWindowEditor != null) _propWindowEditor.Close();
            _propWindowEditor = null;
            if (_propFigEditor != null) _propFigEditor.Close();
            _propFigEditor = null;
            DrawingSurface.CreateShape();
            AddImage.IsEnabled = AddShape.IsEnabled = false;
            SaveContentButton.IsEnabled = true;
            DrawingSurface.EditMode = EditMode.SelectMove;
            ArrowButton.IsChecked = true;
            Title = "Редактор фигур";
            ConnectShapeContextMenu();
            PreviewContentButton.IsEnabled = false;
        }

        private void ConnectShapeContextMenu()
        {
            var cm = new ContextMenu();
            var item = new MenuItem { Header = "Test" };
            cm.Items.Add(item);
            DrawingSurface.ContextMenu = cm;
        }

        private Window _propWindowEditor;
        private Window _propFigEditor;

        private void DrawingSurface_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DrawingSurface.EditMode != EditMode.SelectMove && 
                DrawingSurface.EditMode != EditMode.SelectMultiple)
                return;
            var pointClicked = e.GetPosition(DrawingSurface.GetCanvas);
            var fig = DrawingSurface.GetElementAt(pointClicked);
            UpdatePropsEditorWindow(fig, DrawingSurface.SelectedList, true);
            e.Handled = true;
        }

        private void UpdatePropsEditorWindow(Fig fig, IEnumerable<Fig> list, bool firstCall)
        {
            if (firstCall)
            {
                if (_propWindowEditor == null)
                {
                    switch (DrawingSurface.EditorMode)
                    {
                        case EditorMode.AsScheme:
                            DrawingSurface.Scheme.PropertyChanged +=
                                (o, args) => { SaveContentButton.IsEnabled = true; };
                            _propWindowEditor = new PagePropsEditor(DrawingSurface.Scheme,
                                                                    DrawingSurface.GetFilesFolder())
                                {
                                    Owner = this,
                                    Visibility = Visibility.Hidden
                                };
                            break;
                        case EditorMode.AsShape:
                                DrawingSurface.Shape.PropertyChanged +=
                                    (o, args) => { SaveContentButton.IsEnabled = true; };
                                _propWindowEditor = new ShapePropsEditor(DrawingSurface.Shape)
                                    {
                                        Owner = this, 
                                        Visibility = Visibility.Hidden
                                    };
                            break;
                    }
                }
                if (_propFigEditor == null)
                {
                    _propFigEditor = new FigPropsEditor(DrawingSurface.SelectedFig,
                                                        DrawingSurface.SelectedList)
                        {
                            Owner = this,
                            Visibility = Visibility.Hidden
                        };
                }
            }
            if (_propFigEditor == null || _propWindowEditor == null) return;
            if (fig == null)
            {
                if (_propFigEditor.Visibility == Visibility.Visible || firstCall)
                {
                    _propWindowEditor.Left = _propFigEditor.Left;
                    _propWindowEditor.Top = _propFigEditor.Top;
                    _propWindowEditor.Visibility = Visibility.Visible;
                    _propFigEditor.Visibility = Visibility.Hidden;
                }
            }
            else
            {
                if (_propWindowEditor.Visibility == Visibility.Visible || firstCall)
                {
                    _propFigEditor.Left = _propWindowEditor.Left;
                    _propFigEditor.Top = _propWindowEditor.Top;
                    _propFigEditor.Visibility = Visibility.Visible;
                    _propWindowEditor.Visibility = Visibility.Hidden;
                }
                ((FigPropsEditor)_propFigEditor).UpdateOnSelect(fig, list);
            }
        }

        private void PreviewContentButton_Click(object sender, RoutedEventArgs e)
        {
            var preview = new PreviewSchemeWindow 
            { 
                Owner = this, 
                Width = DrawingSurface.ActualWidth + 16, 
                Height = DrawingSurface.ActualHeight + 50 
            };
            preview.LoadContent(DrawingSurface.FileName);
            preview.ShowDialog();
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            DrawingSurface.EditMode = EditMode.AddLabel;
            AddLabel.IsChecked = true;
        }
    }
}

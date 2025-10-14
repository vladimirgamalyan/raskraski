using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace raskraski
{
    public partial class CategoryView : UserControl
    {
        public ObservableCollection<CategoryItem> Categories { get; set; }
        private MainWindow _main;

        public CategoryView(MainWindow main)
        {
            InitializeComponent();
            _main = main;

            Categories = new ObservableCollection<CategoryItem>();

            string printDir = System.IO.Path.Combine(AppContext.BaseDirectory, ConfigManager.LoadConfig().PictureDir);

            if (Directory.Exists(printDir))
            {
                string[] subdirectories = Directory.GetDirectories(printDir);

                Array.Sort(subdirectories, ExplorerSort.Compare);

                foreach (string d in subdirectories)
                {
                    Categories.Add(new CategoryItem
                    {
                        DirPath = d
                    });
                }
            }

            DataContext = this;
        }

        private void Category_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is CategoryItem category)
            {
                _main.ShowCategoryImages(category.DirPath);
                e.Handled = true; // чтобы событие не "протекало"
            }
        }
    }

    public class CategoryItem : INotifyPropertyChanged
    {
        public required string DirPath { get; set; }

        private BitmapImage? _icon;

        public BitmapImage? Icon
        {
            get
            {
                if (_icon == null) _ = LoadIconAsync();
                return _icon;
            }
            set
            {
                _icon = value;
                OnPropertyChanged();
            }
        }

        private async Task LoadIconAsync()
        {
            string iconFile = System.IO.Path.Combine(DirPath, ".cover.jpg");

            // если нет .cover.jpg — используем встроенный ресурс
            Uri uri = File.Exists(iconFile)
                ? new Uri(iconFile, UriKind.Absolute)
                : new Uri("pack://application:,,,/raskraski;component/Assets/default_cover.jpg", UriKind.Absolute);

            await Task.Run(() =>
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = uri;
                    bmp.DecodePixelWidth = 250;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();

                    Application.Current.Dispatcher.Invoke(() => Icon = bmp);
                }
                catch
                {
                    // игнорируем битые файлы
                }
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

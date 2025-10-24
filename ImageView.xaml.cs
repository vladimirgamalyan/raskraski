using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps;

namespace raskraski
{
    public partial class ImageView : UserControl
    {
        public ObservableCollection<ImageItem> Images { get; set; }
        private MainWindow _main;
        private readonly ImageLoaderQueue _loaderQueue = new();

        public ImageView(MainWindow main, string categoryPath)
        {
            InitializeComponent();
            _main = main;

            Images = new ObservableCollection<ImageItem>();

            var dispatcher = Dispatcher; // UI-диспетчер этого контрола

            if (Directory.Exists(categoryPath))
            {
                string[] supportedExtensions = { ".jpg", ".jpeg", ".png" };

                var imageFiles = Directory.EnumerateFiles(categoryPath, "*.*", SearchOption.TopDirectoryOnly)
                            .Where(f => supportedExtensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                            .Where(f => !f.EndsWith(".cover.jpg", StringComparison.OrdinalIgnoreCase))
                            .OrderBy(f => f, ExplorerSort.Comparer)
                            .ToArray();

                foreach (var file in imageFiles)
                    Images.Add(new ImageItem(file, dispatcher));
            }

            DataContext = this;

            Loaded += (_, _) => InitializeScrollTracking();
        }

        private void InitializeScrollTracking()
        {
            var scrollViewer = FindVisualChild<ScrollViewer>(ImagesListBox);
            if (scrollViewer == null)
                return;

            scrollViewer.ScrollChanged += (_, __) =>
            {
                // немедленная подгрузка при движении
                ScheduleVisibleLoads(scrollViewer);
            };

            // начальная загрузка
            ScheduleVisibleLoads(scrollViewer);
        }

        private double? _cellWidth;
        private double? _cellHeight;

        private void ScheduleVisibleLoads(ScrollViewer scrollViewer)
        {
            var listBox = ImagesListBox;
            if (listBox == null || Images.Count == 0)
                return;

            // Если ещё не знаем размеры ячейки — пробуем вычислить
            if (_cellWidth == null || _cellHeight == null)
            {
                // Берём первый визуально сгенерированный элемент
                var firstContainer = listBox.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                if (firstContainer != null)
                {
                    _cellWidth = firstContainer.ActualWidth;
                    _cellHeight = firstContainer.ActualHeight;
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[ImageView] Cell size auto-calibrated: {_cellWidth} × {_cellHeight}");
#endif
                }
                else
                {
                    // Контейнеры ещё не созданы — откладываем вызов
                    return;
                }
            }

            double cellWidth = _cellWidth ?? ConfigManager.LoadConfig().PreviewWidth;
            double cellHeight = _cellHeight ?? ConfigManager.LoadConfig().PreviewHeight;

            // Ширина окна просмотра
            double viewportWidth = scrollViewer.ViewportWidth;
            if (viewportWidth <= 0)
                return;

            // Сколько элементов в одной строке
            int itemsPerRow = Math.Max(1, (int)(viewportWidth / cellWidth));

            // Расчёт диапазона строк по позиции прокрутки
            double prefetchMargin = scrollViewer.ViewportHeight * 0.5;
            double offsetY = scrollViewer.VerticalOffset;

            int firstVisibleRow = Math.Max(0, (int)((offsetY - prefetchMargin) / cellHeight));
            int lastVisibleRow = (int)Math.Ceiling((offsetY + scrollViewer.ViewportHeight + prefetchMargin) / cellHeight);

            int firstIndex = firstVisibleRow * itemsPerRow;
            int lastIndex = Math.Min(Images.Count - 1, (lastVisibleRow + 1) * itemsPerRow - 1);

            // Центр экрана (для приоритета)
            double viewportCenterY = offsetY + scrollViewer.ViewportHeight / 2.0;

            var targets = new List<(ImageItem, double)>();

            for (int i = firstIndex; i <= lastIndex && i < Images.Count; i++)
            {
                if (Images[i].Bitmap != null)
                    continue;

                // Расстояние от центра экрана (для приоритета)
                int row = i / itemsPerRow;
                double centerY = row * cellHeight + cellHeight / 2.0;
                double priority = Math.Abs(centerY - viewportCenterY);

                targets.Add((Images[i], priority));
            }

            _loaderQueue.UpdateTargets(targets);
        }


        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _main.BackToCategories();
        }
        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img && img.DataContext is ImageItem imageItem)
            {
                if (imageItem.IsPrinted)
                    return;

                string fileToPrint = imageItem.FilePath;

                imageItem.IsPrinted = true;

                PrintedStore.MarkPrinted(imageItem.FilePath);

                // кладём в очередь
                PrintQueueManager.Enqueue(new PrintJob
                {
                    FilePath = imageItem.FilePath,
                });
            }
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (e.XButton1 == MouseButtonState.Pressed) // кнопка "Назад"
            {
                BackButton_Click(this, new RoutedEventArgs());
                e.Handled = true; // предотвратить прокидывание дальше
            }
        }
    }
}

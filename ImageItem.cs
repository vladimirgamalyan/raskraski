using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace raskraski
{
    public class ImageItem : INotifyPropertyChanged
    {
        private readonly Dispatcher _dispatcher;
        public string FilePath { get; }

        private BitmapImage? _bitmap;
        public BitmapImage? Bitmap
        {
            get => _bitmap;
            private set
            {
                _bitmap = value;
                OnPropertyChanged();
            }
        }

        private bool _isLoaded;
        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                if (_isLoaded != value)
                {
                    _isLoaded = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPrinted;
        public bool IsPrinted
        {
            get => _isPrinted;
            set
            {
                if (_isPrinted != value)
                {
                    _isPrinted = value;
                    OnPropertyChanged();
                }
            }
        }

        public ImageItem(string filePath, Dispatcher dispatcher)
        {
            FilePath = filePath;
            _dispatcher = dispatcher;
            _isPrinted = PrintedStore.IsPrinted(FilePath);
        }

        #region --- Static Cache ---

        private const int MaxCacheSize = 500;

        private static readonly ConcurrentDictionary<string, BitmapImage> _cache = new();
        private static readonly LinkedList<string> _lruList = new(); // хранит порядок использования
        private static readonly object _cacheLock = new();

        /// <summary>
        /// Возвращает кэшированное изображение, если оно есть.
        /// </summary>
        private static bool TryGetFromCache(string path, out BitmapImage? image)
        {
            if (_cache.TryGetValue(path, out image))
            {
                lock (_cacheLock)
                {
                    // перемещаем элемент в начало (недавно использован)
                    _lruList.Remove(path);
                    _lruList.AddFirst(path);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Добавляет изображение в кэш с контролем размера.
        /// </summary>
        private static void AddToCache(string path, BitmapImage image)
        {
            lock (_cacheLock)
            {
                if (_cache.TryAdd(path, image))
                {
                    _lruList.AddFirst(path);

                    // если кэш переполнен — удалить самый старый
                    if (_lruList.Count > MaxCacheSize)
                    {
                        string oldest = _lruList.Last!.Value;
                        _lruList.RemoveLast();
                        _cache.TryRemove(oldest, out _);
                    }
                }
            }
        }

        /// <summary>
        /// Полностью очищает кэш.
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        #endregion

        /// <summary>
        /// Асинхронная загрузка изображения с использованием LRU-кэша.
        /// </summary>
        public async Task LoadAsyncPublic()
        {
            if (!File.Exists(FilePath))
                return;

            int PreviewWidth = ConfigManager.LoadConfig().PreviewWidth;
            int PreviewHeight = ConfigManager.LoadConfig().PreviewHeight;

            try
            {
                // 🔹 Проверяем кэш
                if (TryGetFromCache(FilePath, out var cachedBmp) && cachedBmp != null)
                {
                    _dispatcher.Invoke(() =>
                    {
                        Bitmap = cachedBmp;
                        IsLoaded = true;
                    });
                    return;
                }

                // 🔹 Загружаем с диска в фоне
                var bmp = await Task.Run(() =>
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(FilePath);
                    image.DecodePixelWidth = PreviewWidth;
                    image.DecodePixelHeight = PreviewHeight;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.EndInit();
                    image.Freeze();

                    AddToCache(FilePath, image);
                    return image;
                });

                _dispatcher.Invoke(() =>
                {
                    Bitmap = bmp;
                    IsLoaded = true;
                });
            }
            catch
            {
                // игнорируем битые файлы
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

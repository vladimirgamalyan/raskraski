using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace raskraski
{
    public class ImageLoaderQueue
    {
        private readonly SemaphoreSlim _semaphore = new(4, 4);
        private readonly ConcurrentDictionary<string, Task> _activeTasks = new();
        private readonly HashSet<string> _desired = new(); // какие картинки должны быть загружены
        private readonly object _sync = new();

        public void UpdateTargets(IEnumerable<(ImageItem item, double priority)> targets)
        {
            var targetPaths = targets.Select(t => t.item.FilePath).ToHashSet();

            lock (_sync)
            {
                // 1️ Удаляем из списка "желаемых" те, кто больше не нужен
                _desired.RemoveWhere(p => !targetPaths.Contains(p));

                // 2️ Добавляем новые цели
                foreach (var t in targets)
                {
                    if (!_desired.Contains(t.item.FilePath))
                    {
                        _desired.Add(t.item.FilePath);
                        StartLoadTask(t.item, t.priority);
                    }
                }
            }
        }

        private void StartLoadTask(ImageItem item, double priority)
        {
            if (item.Bitmap != null)
                return;

            // если уже загружается — ничего не делаем
            if (_activeTasks.ContainsKey(item.FilePath))
                return;

            // запускаем задачу
            var task = Task.Run(async () =>
            {
                try
                {
                    await _semaphore.WaitAsync();

                    // проверяем: всё ещё в списке "желаемых"?
                    lock (_sync)
                    {
                        if (!_desired.Contains(item.FilePath))
                            return; // уже не нужно
                    }

                    await item.LoadAsyncPublic();
                }
                catch
                {
                    // игнорируем ошибки загрузки
                }
                finally
                {
                    _semaphore.Release();
                    _activeTasks.TryRemove(item.FilePath, out _);
                }
            });

            _activeTasks[item.FilePath] = task;
        }
    }
}

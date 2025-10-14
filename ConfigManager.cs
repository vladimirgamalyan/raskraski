using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace raskraski
{
    public static class ConfigManager
    {
        private static AppConfig? _cachedConfig;
        private static DateTime _lastReadTime;

        public static string GetFileName()
        {
            string exeDir = AppContext.BaseDirectory;
            string configPath = Path.Combine(exeDir, "config.json");
            return configPath;
        }

        public static AppConfig LoadConfig()
        {
            string _file = GetFileName();

            // если уже загружен и файл не менялся — возвращаем из кеша
            if (_cachedConfig != null && File.Exists(_file))
            {
                DateTime lastWrite = File.GetLastWriteTime(_file);
                if (lastWrite == _lastReadTime)
                    return _cachedConfig;
            }

            // иначе читаем заново
            //if (!File.Exists(_file))
            //{
            //    _cachedConfig = new AppConfig { RootPath = "D:\\Categories" };
            //    return _cachedConfig;
            //}

            string json = File.ReadAllText(_file);
            _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            _lastReadTime = File.GetLastWriteTime(_file);

            return _cachedConfig;
        }

        public static void Reload()
        {
            _cachedConfig = null;
            LoadConfig();
        }
    }
}

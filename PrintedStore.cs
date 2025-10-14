using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace raskraski
{
    public static class PrintedStore
    {
        // ключ формата "groupId:id"
        private static readonly Dictionary<string, bool> _printed = new();

        private const string FileName = "printed.json";

        public static bool IsPrinted(string filePath)
        {
            return _printed.TryGetValue(filePath, out bool value) && value;
        }

        public static void MarkPrinted(string filePath)
        {
            _printed[filePath] = true;
            Save();
        }

        public static void ResetPrinted(string filePath)
        {
            _printed[filePath] = false;
            Save();
        }

        public static void Save()
        {
            //var json = JsonSerializer.Serialize(_printed, new JsonSerializerOptions { WriteIndented = true });
            //File.WriteAllText(FileName, json);
        }

        public static void Load()
        {
            if (File.Exists(FileName))
            {
                var json = File.ReadAllText(FileName);
                var dict = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                if (dict != null)
                {
                    _printed.Clear();
                    foreach (var kv in dict)
                        _printed[kv.Key] = kv.Value;
                }
            }
        }
    }
}

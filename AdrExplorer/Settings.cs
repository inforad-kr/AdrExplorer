using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AdrExplorer
{
    class Settings
    {
        public string ServerUrl { get; set; } = "https://demo.xpacs.iberisoft.com:5201";

        public string UserName { get; set; }

        public string Password { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Today.AddYears(-5);

        public DateTime EndDate { get; set; } = DateTime.Today;

        public bool PendingOnly { get; set; }

        public bool Overwrite { get; set; }

        public int StudyCount { get; set; } = 5000;

        public Dictionary<string, string> CustomStrings { get; set; }

        public static Settings Load()
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath));
            }
            catch
            {
                var settings = new Settings();
                settings.Save();
                return settings;
            }
        }

        public void Save() => File.WriteAllText(FilePath, JsonSerializer.Serialize(this));

        private static string FilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameof(Settings) + ".json");
    }
}

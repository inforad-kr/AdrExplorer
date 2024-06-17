using System.IO;
using System.Text.Json;

namespace AdrExplorer.Hhi
{
    class Settings
    {
        public string ProcessPath { get; set; }

        public string ProcessArgs { get; set; }

        public static Settings Load()
        {
            try
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath));
            }
            catch
            {
                return new();
            }
        }

        private static string FilePath => Path.ChangeExtension(typeof(Settings).Assembly.Location, ".json");
    }
}

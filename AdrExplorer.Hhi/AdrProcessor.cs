using AdrExplorer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdrExplorer.Hhi;

class AdrProcessor : IAdrProcessor
{
    readonly string m_FolderPath;
    Dictionary<int, bool> m_Results;

    public AdrProcessor()
    {
        m_FolderPath = Path.Combine(Path.GetTempPath(), "AdrExplorer");
        Directory.CreateDirectory(m_FolderPath);
        foreach (var filePath in Directory.EnumerateFiles(m_FolderPath, "*.jpg"))
        {
            File.Delete(filePath);
        }
    }

    public void LoadFile(int id, byte[] data)
    {
        File.WriteAllBytes(Path.Combine(m_FolderPath, $"{id}.jpg"), data);
    }

    public async Task ProcessFiles()
    {
        var settings = Settings.Load();
        var info = new ProcessStartInfo
        {
            FileName = settings.ProcessPath,
            Arguments = string.Format(settings.ProcessArgs, m_FolderPath),
            RedirectStandardOutput = true
        };
        using var process = Process.Start(info);
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Error #{process.ExitCode}");
        }

        var output = process.StandardOutput.ReadToEnd();
        var response = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)[^1];
        response = response.Replace('\'', '"');
        using var document = JsonDocument.Parse(response);
        var element = document.RootElement.GetProperty("results");
        m_Results = element.EnumerateArray().ToDictionary(
            element => int.Parse(Path.GetFileNameWithoutExtension(element.GetProperty("image_name").GetString())),
            element => element.GetProperty("status").GetInt32() != 0);
    }

    public bool? GetResult(int id) => m_Results?.TryGetValue(id, out var flag) == true ? flag : null;
}

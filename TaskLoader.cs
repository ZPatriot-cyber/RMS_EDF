using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace RMS_EDF;

public static class TaskLoader
{
    public static List<PeriodicTask> Load(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".json" => LoadJson(path),
            ".csv" => LoadCsv(path),
            _ => throw new ArgumentException(
                $"Неподдерживаемый формат файла: {ext}. Используйте .json или .csv")
        };
    }

    private static List<PeriodicTask> LoadJson(string path)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var raw = JsonSerializer.Deserialize<List<PeriodicTask>>(
                      File.ReadAllText(path), options)
                  ?? throw new InvalidDataException("Пустой JSON");
        return raw;
    }

    private static List<PeriodicTask> LoadCsv(string path)
    {
        var lines = File.ReadAllLines(path)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();
        var result = new List<PeriodicTask>();
        // Первая строка — заголовок
        for (int i = 1; i < lines.Count; i++)
        {
            var parts = lines[i].Split(',');
            if (parts.Length < 4) continue;
            result.Add(new PeriodicTask(
                parts[0].Trim(),
                double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                double.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                double.Parse(parts[3].Trim(), CultureInfo.InvariantCulture)));
        }
        return result;
    }
}
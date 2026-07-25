using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClassRollCall.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, object> _settings;
    private readonly string _filePath;

    public ConfigurationService()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClassRollCall");
        Directory.CreateDirectory(appDataPath);
        _filePath = Path.Combine(appDataPath, "appsettings.json");

        _settings = new Dictionary<string, object>();
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            string json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            if (loaded == null) return;

            foreach (var kvp in loaded)
            {
                _settings[kvp.Key] = kvp.Value;
            }
        }
        catch
        {
            // 加载失败则使用空配置
        }
    }

    public T? GetConfiguration<T>(string key)
    {
        if (_settings.TryGetValue(key, out object? value) && value is JsonElement element)
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        }
        return default;
    }

    public void SetConfiguration<T>(string key, T value)
    {
        string json = JsonSerializer.Serialize(value);
        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        _settings[key] = element;
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(_settings, options);
        File.WriteAllText(_filePath, json);
    }
}
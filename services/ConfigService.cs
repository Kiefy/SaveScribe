using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic; // <-- Fixes the 'List' errors
using SaveScribe.Models;

namespace SaveScribe.Services;

public class ConfigService
{
    // Path.Combine and AppDomain require 'using System;'
    private readonly string _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "savescribe_config.json");
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public List<GameConfig> LoadConfigs()
    {
        if (!File.Exists(_configPath)) return new List<GameConfig>();
        
        try
        {
            string jsonString = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<List<GameConfig>>(jsonString) ?? new List<GameConfig>();
        }
        catch
        {
            return new List<GameConfig>();
        }
    }

    public void SaveConfigs(List<GameConfig> configs)
    {
        string jsonString = JsonSerializer.Serialize(configs, _jsonOptions);
        File.WriteAllText(_configPath, jsonString);
    }
}
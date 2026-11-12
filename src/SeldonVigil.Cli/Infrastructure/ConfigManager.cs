using System.Text.Json;

namespace SeldonVigil.Cli.Infrastructure;

internal sealed class ConfigManager
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".seldonvigil");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public static ConfigManager Load()
    {
        if (!File.Exists(ConfigPath))
        {
            return new ConfigManager();
        }

        var json = File.ReadAllText(ConfigPath);
        return JsonSerializer.Deserialize<ConfigManager>(json, JsonOptions) ?? new ConfigManager();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }

    public void Clear()
    {
        ApiKey = null;
        AccessToken = null;
        RefreshToken = null;
        TokenExpiresAt = null;
        Save();
    }
}

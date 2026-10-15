using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage.Streams;

namespace MagicCursor;

public class ConfigData
{
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiApiKeyProtected { get; set; } = string.Empty;
    public bool RunAtStartup { get; set; } = false;
    public string TextModel { get; set; } = "gemini-3.5-flash-lite";
    public string ImageModel { get; set; } = "gemini-3.6-flash";
}

public static class ConfigService
{
    private static readonly string FolderPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MagicCursor"
    );

    private static readonly string FilePath = Path.Combine(FolderPath, "config.json");

    public static ConfigData LoadConfig()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var config = JsonSerializer.Deserialize<ConfigData>(json);
                if (config != null)
                {
                    return config;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Config load failed", ex);
        }

        return new ConfigData();
    }

    public static void SaveConfig(ConfigData config)
    {
        try
        {
            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json);
        }
        catch (Exception ex)
        {
            Log.Error("Config save failed", ex);
        }
    }

    public static async Task<string> ProtectStringAsync(string clearText)
    {
        if (string.IsNullOrEmpty(clearText)) return string.Empty;
        try
        {
            var provider = new DataProtectionProvider("LOCAL=user");
            IBuffer buffClear = CryptographicBuffer.ConvertStringToBinary(clearText, BinaryStringEncoding.Utf8);
            IBuffer buffProtected = await provider.ProtectAsync(buffClear);
            return CryptographicBuffer.EncodeToBase64String(buffProtected);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to protect API key", ex);
            return string.Empty;
        }
    }

    public static async Task<string> UnprotectStringAsync(string base64Protected)
    {
        if (string.IsNullOrEmpty(base64Protected)) return string.Empty;
        try
        {
            var provider = new DataProtectionProvider();
            IBuffer buffProtected = CryptographicBuffer.DecodeFromBase64String(base64Protected);
            IBuffer buffClear = await provider.UnprotectAsync(buffProtected);
            return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, buffClear);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to unprotect API key", ex);
            return string.Empty;
        }
    }

    public static async Task<string> GetApiKeyAsync(ConfigData config)
    {
        if (!string.IsNullOrEmpty(config.GeminiApiKeyProtected))
        {
            return await UnprotectStringAsync(config.GeminiApiKeyProtected);
        }

        // Migration: If legacy plaintext key is present, encrypt and clear legacy field
        if (!string.IsNullOrEmpty(config.GeminiApiKey))
        {
            string key = config.GeminiApiKey;
            config.GeminiApiKeyProtected = await ProtectStringAsync(key);
            config.GeminiApiKey = string.Empty;
            SaveConfig(config);
            return key;
        }

        return string.Empty;
    }

    public static string GetApiKey(ConfigData config)
    {
        return Task.Run(() => GetApiKeyAsync(config)).GetAwaiter().GetResult();
    }

    public static async Task SetApiKeyAsync(ConfigData config, string apiKey)
    {
        config.GeminiApiKey = string.Empty;
        config.GeminiApiKeyProtected = await ProtectStringAsync(apiKey);
        SaveConfig(config);
    }
}

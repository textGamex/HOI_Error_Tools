using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic;

public sealed class GlobalSettings
{
    public static readonly string SettingsFolderPath = Path.Combine(Environment.CurrentDirectory, "Settings");
    public HashSet<ErrorCode> InhibitedErrorCodes { get; }
    public HashSet<ErrorType> InhibitedErrorTypes { get; }
    public bool EnableParseCompletionPrompt { get; set; } = true;
    public bool EnableAutoCheckUpdate { get; set; } = true;
    public bool EnableAppCenter { get; set; } = true;

    private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "MainSettings.json");

    public GlobalSettings(HashSet<ErrorCode> inhibitedErrorCodes, HashSet<ErrorType> inhibitedErrorTypes, bool enableParseCompletionPrompt, bool enableAutoCheckUpdate, bool enableAppCenter)
    {
        InhibitedErrorCodes = inhibitedErrorCodes;
        InhibitedErrorTypes = inhibitedErrorTypes;
        EnableParseCompletionPrompt = enableParseCompletionPrompt;
        EnableAutoCheckUpdate = enableAutoCheckUpdate;
        EnableAppCenter = enableAppCenter;
    }

    private GlobalSettings()
    {
        InhibitedErrorCodes = new HashSet<ErrorCode>();
        InhibitedErrorTypes = new HashSet<ErrorType>();
    }

    public static GlobalSettings Load()
    {
        if (File.Exists(SettingsFilePath))
        {
            return JsonSerializer.Deserialize<GlobalSettings>(File.ReadAllText(SettingsFilePath)) ??
                   throw new ArgumentNullException(SettingsFilePath);
        }
        else
        {
            return new GlobalSettings();
        }
    }

    public Task SaveAsync()
    {
        if (!Directory.Exists(SettingsFolderPath))
        {
             _ = Directory.CreateDirectory(SettingsFolderPath);
        }
        var options = new JsonSerializerOptions { WriteIndented = true };
        return File.WriteAllTextAsync(SettingsFilePath, JsonSerializer.Serialize(this, options), 
            Encoding.UTF8);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HOI_Error_Tools.Logic.Analyzers.Error;
using Newtonsoft.Json;

namespace HOI_Error_Tools.Logic;

public sealed class GlobalSettings
{
    public static readonly string SettingsFolderPath = Path.Combine(Environment.CurrentDirectory, "Settings");
    public HashSet<ErrorCode> InhibitedErrorCodes { get; }
    public HashSet<ErrorType> InhibitedErrorTypes { get; }
    public bool EnableParseCompletionPrompt { get; set; } = true;

    private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "MainSettings.json");
    public static GlobalSettings Load()
    {
        if (File.Exists(SettingsFilePath))
        {
            return JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText(SettingsFilePath)) ?? throw new ArgumentNullException();
        }
        else
        {
             return new GlobalSettings();
        }
    }

    private GlobalSettings()
    {
        InhibitedErrorCodes = new HashSet<ErrorCode>();
        InhibitedErrorTypes = new HashSet<ErrorType>();
    }

    public void Save()
    {
        if (!Directory.Exists(SettingsFolderPath))
        {
            Directory.CreateDirectory(SettingsFolderPath);
        }
        File.WriteAllTextAsync(SettingsFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
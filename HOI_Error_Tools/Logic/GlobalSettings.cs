﻿using System;
using System.Collections.Generic;
using System.IO;
using HOI_Error_Tools.Logic.Analyzers.Error;
using Newtonsoft.Json;

namespace HOI_Error_Tools.Logic;

public sealed class GlobalSettings
{
    public static readonly string SettingsFolderPath = Path.Combine(Environment.CurrentDirectory, "Settings");
    private static readonly string SettingsFilePath = Path.Combine(SettingsFolderPath, "MainSettings.json");
    public static GlobalSettings Settings { get; } = Load();
    public HashSet<ErrorCode> InhibitedErrorCodes { get; }

    private static GlobalSettings Load()
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
    }

    public void Save()
    {
        File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(this));
    }
}
﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic;
using NLog;


namespace HOI_Error_Tools.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    private readonly ILogger _log;
    private readonly GlobalSettings _settings;

    public SettingsWindowViewModel(ILogger log, GlobalSettings settings)
    {
        _log = log;
        _settings = settings;
    }

    [RelayCommand]
    private void WindowClosed()
    {
        Task.Run(async () => await _settings.SaveAsync());
        _log.Info(CultureInfo.InvariantCulture,
            "设置已保存到: {Path}", GlobalSettings.SettingsFolderPath.ToFilePath());
    }
}
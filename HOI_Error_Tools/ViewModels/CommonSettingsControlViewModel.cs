using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Services;
using Microsoft.AppCenter.Analytics;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class CommonSettingsControlViewModel : ObservableObject
{
    private readonly GlobalSettings _globalSettings;
    private readonly ILogger _log;

    [ObservableProperty] 
    private bool _enableParseCompletionPrompt;

    [ObservableProperty] 
    private bool _enableAutoCheckUpdate;

    [ObservableProperty]
    private bool _enableAppCenter;

    [ObservableProperty] 
    private string _logFilesSize;

    private readonly IMessageBox _messageBox;

    public CommonSettingsControlViewModel(GlobalSettings globalSettings, ILogger log, IMessageBox messageBox)
    {
        _globalSettings = globalSettings;
        EnableParseCompletionPrompt = _globalSettings.EnableParseCompletionPrompt;
        EnableAutoCheckUpdate = _globalSettings.EnableAutoCheckUpdate;
        EnableAppCenter = _globalSettings.EnableAppCenter;
        _log = log;
        _messageBox = messageBox;
        LogFilesSize = GetLogFilesSize();

        PropertyChanged += CommonSettingsControlViewModel_PropertyChanged;
    }

    private void CommonSettingsControlViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(EnableParseCompletionPrompt):
                _globalSettings.EnableParseCompletionPrompt = EnableParseCompletionPrompt;
                break;
            case nameof(EnableAutoCheckUpdate):
                _globalSettings.EnableAutoCheckUpdate = EnableAutoCheckUpdate;
                break;
            case nameof(EnableAppCenter):
                _globalSettings.EnableAppCenter =  EnableAppCenter;
                break;
        }

        _log.Debug(CultureInfo.InvariantCulture, "Changed value: {ValueName} to {Value}", e.PropertyName,
            typeof(CommonSettingsControlViewModel).GetProperty(e.PropertyName ?? string.Empty)?.GetValue(this));
    }

    private static string GetLogFilesSize()
    {
        var filesSize = FileHelper.GetFilesSizeInBytes(App.LogsFolderPath);
        var byteSize = ByteSize.FromBytes(filesSize);
        if (byteSize.MebiBytes >= 1.0)
        {
            return $"约占用 {byteSize.MebiBytes:F2} MB";
        }

        return $"约占用 {byteSize.KibiBytes:F1} KB";
    }

    [RelayCommand]
    private void ClearLogsFolder()
    {
        foreach (var filePath in Directory.GetFiles(App.LogsFolderPath))
        {
            try
            {
                File.Delete(filePath);
                _log.Debug(CultureInfo.InvariantCulture, "Delete file: {Path}", filePath);
            }
            catch (UnauthorizedAccessException e)
            {
                _log.Warn(e);
                _messageBox.ErrorTip($"文件: {filePath} 删除失败");
            }
        }
        
        LogFilesSize = GetLogFilesSize();
        Analytics.TrackEvent("Clear Logs folder");
    }
}
using System.ComponentModel;
using System.Globalization;
using System.IO;
using ByteSizeLib;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.Util;
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
    private string _logFilesSize;

    public CommonSettingsControlViewModel(GlobalSettings globalSettings, ILogger log)
    {
        _globalSettings = globalSettings;
        EnableParseCompletionPrompt = _globalSettings.EnableParseCompletionPrompt;
        EnableAutoCheckUpdate = _globalSettings.EnableAutoCheckUpdate;
        _log = log;
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
            File.Delete(filePath);
            _log.Debug(CultureInfo.InvariantCulture, "Delete file: {Path}", filePath);
        }

        LogFilesSize = GetLogFilesSize();
    }
}
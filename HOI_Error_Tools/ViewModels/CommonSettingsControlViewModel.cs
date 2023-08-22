using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI_Error_Tools.Logic;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class CommonSettingsControlViewModel : ObservableObject
{
    private readonly GlobalSettings _globalSettings;
    private readonly ILogger _log;

    [ObservableProperty]
    private bool _enableParseCompletionPrompt;


    public CommonSettingsControlViewModel(GlobalSettings globalSettings, ILogger log)
    {
        PropertyChanged += CommonSettingsControlViewModel_PropertyChanged;
        _globalSettings = globalSettings;
        _log = log;
        EnableParseCompletionPrompt = _globalSettings.EnableParseCompletionPrompt;
    }

    private void CommonSettingsControlViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EnableParseCompletionPrompt))
        {
            _globalSettings.EnableParseCompletionPrompt = EnableParseCompletionPrompt;
        }
        _globalSettings.Save();
        _log.Debug(CultureInfo.InvariantCulture, "Changed value: {Value}", e.PropertyName);
        _log.Info("保存全局设置");
    }
}
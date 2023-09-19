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
    [ObservableProperty]
    private bool _enableAutoCheckUpdate;

    public CommonSettingsControlViewModel(GlobalSettings globalSettings, ILogger log)
    {
        _globalSettings = globalSettings;
        EnableParseCompletionPrompt = _globalSettings.EnableParseCompletionPrompt;
        EnableAutoCheckUpdate = _globalSettings.EnableAutoCheckUpdate;
        _log = log;

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

        _log.Debug(CultureInfo.InvariantCulture, "Changed value: {Value}", e.PropertyName);
    }
}
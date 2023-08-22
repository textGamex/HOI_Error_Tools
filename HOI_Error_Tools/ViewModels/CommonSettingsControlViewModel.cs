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
        _globalSettings = globalSettings;
        EnableParseCompletionPrompt = _globalSettings.EnableParseCompletionPrompt;
        _log = log;

        PropertyChanged += CommonSettingsControlViewModel_PropertyChanged;
    }

    private void CommonSettingsControlViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EnableParseCompletionPrompt))
        {
            _globalSettings.EnableParseCompletionPrompt = EnableParseCompletionPrompt;
        }
        _log.Debug(CultureInfo.InvariantCulture, "Changed value: {Value}", e.PropertyName);
    }
}
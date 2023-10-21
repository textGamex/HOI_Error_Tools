using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic;
using NLog;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using HOI_Error_Tools.Logic.Util;
using Humanizer;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageSettingsControlViewModel : ObservableObject
{
    [ObservableProperty]
    private List<SettingsWindowViewVO> _data;

    private readonly GlobalSettings _settings;
    private readonly List<ErrorCode> _changedErrorCodes;
    private readonly ILogger _log;

    public ErrorMessageSettingsControlViewModel(GlobalSettings settings, ILogger logger)
    {
        _log = logger;
        _settings = settings;
        _changedErrorCodes = new List<ErrorCode>(8);
        _data = GetViewVOs();
    }

    [RelayCommand]
    private void IgnoreErrorCheckBox(uint code)
    {
        var errorCode = (ErrorCode)code;
        if (_changedErrorCodes.Contains(errorCode))
        {
            _changedErrorCodes.Remove(errorCode);
        }
        else
        {
            _changedErrorCodes.Add(errorCode);
        }
    }

    private void ProcessChangedErrorCodes()
    {
        if (_changedErrorCodes.Count == 0)
        {
            return;
        }
        foreach (var code in _changedErrorCodes)
        {
            if (_settings.InhibitedErrorCodes.Contains(code))
            {
                _settings.InhibitedErrorCodes.Remove(code);
            }
            else
            {
                _settings.InhibitedErrorCodes.Add(code);
            }
        }
        _log.Debug(CultureInfo.InvariantCulture,
            "Changed codes: {ChangedErrorCodes}", string.Join(", ", _changedErrorCodes));
        _changedErrorCodes.Clear();
    }

    [RelayCommand]
    private void SaveButton()
    {
        if (_changedErrorCodes.Count == 0)
        {
            return;
        }
        ProcessChangedErrorCodes();
        Task.Run(() => _settings.SaveAsync());

        ToastService.Push("设置已保存");
    }

    [RelayCommand]
    private void ResetButton()
    {
        _changedErrorCodes.Clear();
        Data = GetViewVOs();
        _log.Debug("Reset button clicked");
    }

    private List<SettingsWindowViewVO> GetViewVOs()
    {
        var values = Enums.GetValues<ErrorCode>();
        var list = new List<SettingsWindowViewVO>(values.Count);
        foreach (var code in values)
        {
            list.Add(new SettingsWindowViewVO(
                Enums.ToUInt32(code), code.Humanize(), _settings.InhibitedErrorCodes.Contains(code)));
        }

        return list;
    }

    [RelayCommand]
    private void ControlUnloaded()
    {
        _log.Debug("ErrorMessageSettingsControlViewModel unloaded event.");
        if (_changedErrorCodes.Count == 0)
        {
            return;
        }
        if (MessageBox.Show("您还没有保存更改, 是否现在保存?", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            SaveButton();
        }
    }

    public record SettingsWindowViewVO(uint Code, string Message, bool IsChecked);
}
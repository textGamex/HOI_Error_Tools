using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Analyzers.Error;
using Humanizer;
using Microsoft.Toolkit.Uwp.Notifications;
using NLog;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HOI_Error_Tools.ViewModels;

public partial class SettingsWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private List<SettingsWindowViewVO> _data;

    private readonly GlobalSettings _settings;
    private readonly List<ErrorCode> _changedErrorCodes;
    private readonly ILogger _log;

    public SettingsWindowViewModel(GlobalSettings settings, ILogger logger)
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
        _settings.Save();

        new ToastContentBuilder()
            .AddText("设置已保存")
            .Show();
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
    private void WindowClosing()
    {
        _log.Debug("Settings window closing event");
        if (_changedErrorCodes.Count == 0)
        {
            return;
        }
        if (MessageBox.Ask("您还没有保存更改, 是否现在保存?") == MessageBoxResult.OK)
        {
            SaveButton();
        }
    }

    public record SettingsWindowViewVO(uint Code, string Message, bool IsChecked);
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.View;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    public IReadOnlyList<ErrorMessage> ErrorMessage { get; }
    public string StatisticsInfo { get; }
    public string ParseDateTime { get; }

    private readonly IErrorFileInfoService _errorFileInfoService;

    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public ErrorMessageWindowViewModel(
        IErrorMessageService errorMessageService,
        GlobalSettings settings,
        IErrorFileInfoService errorFileInfoService)
    {
        _errorFileInfoService = errorFileInfoService;
        var errors = errorMessageService.GetErrorMessages();
        var rawCount = errors.Count;
        ErrorMessage = errors.Where(item => !settings.InhibitedErrorCodes.Contains(item.Code)).ToList();
        StatisticsInfo = $"错误 {ErrorMessage.Count}, 忽略 {rawCount - ErrorMessage.Count}";
        ParseDateTime = $"报告生成时间: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}";
    }

    [RelayCommand]
    private void ShowErrorFileInfo(IEnumerable<ParameterFileInfo> obj)
    {
        _errorFileInfoService.SetFileErrorInfoList(obj.ToList());
        var errorFileInfoWindow = App.Current.Services.GetRequiredService<ErrorFileInfoView>();
        errorFileInfoWindow.Show();
        _errorFileInfoService.Clear();

        Log.Debug("ErrorFileInfoView window start");
    }
}
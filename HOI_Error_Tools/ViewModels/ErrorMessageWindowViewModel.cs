using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.View;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Services;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    public ObservableCollection<ErrorMessageWindowViewModelVo> ErrorMessage { get; }
    public string IgnoredErrorCount { get; }
    public string ParseDateTime { get; }
    public string DisplayedErrorCount => $"错误: {ErrorMessage.Count}";
    public string DeleteMenuItemHeader => _selectedItems.Count == ErrorMessage.Count ? "清空" : $"删除 {_selectedItems.Count} 项";

    private IReadOnlyList<ErrorMessageWindowViewModelVo> _selectedItems = Array.Empty<ErrorMessageWindowViewModelVo>();
    private readonly IErrorFileInfoService _errorFileInfoService;
    private readonly GlobalSettings _settings;

    //private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public ErrorMessageWindowViewModel(
        IErrorMessageService errorMessageService,
        GlobalSettings settings,
        IErrorFileInfoService errorFileInfoService)
    {
        _errorFileInfoService = errorFileInfoService;
        _settings = settings;

        var errors = errorMessageService.GetErrorMessages();
        var rawCount = errors.Count;

        ErrorMessage = new ObservableCollection<ErrorMessageWindowViewModelVo>(
            errors.Where(IsAllowedShow)
            .Select(message => new ErrorMessageWindowViewModelVo(message)));

        IgnoredErrorCount = $"忽略: {rawCount - ErrorMessage.Count}";
        ParseDateTime = $"报告生成时间: {DateTime.Now.ToString(CultureInfo.CurrentCulture)}";

        ErrorMessage.CollectionChanged += (_, _) => OnPropertyChanged(nameof(DisplayedErrorCount));
    }

    private bool IsAllowedShow(ErrorMessage error)
    {
        return !_settings.InhibitedErrorCodes.Contains(error.Code) &&
               !_settings.InhibitedErrorTypes.Contains(error.Type);
    }

    [RelayCommand]
    private void ShowErrorFileInfo(IEnumerable<ParameterFileInfo> obj)
    {
        _errorFileInfoService.SetFileErrorInfoList(obj.ToList());
        var errorFileInfoWindow = App.Current.Services.GetRequiredService<ErrorFileInfoView>();
        errorFileInfoWindow.Show();
        _errorFileInfoService.Clear();
    }

    [RelayCommand]
    private void DeleteSelectedErrorMessage(IList list)
    {
        if (list.Count == ErrorMessage.Count)
        {
            ErrorMessage.Clear();
            return;
        }

        foreach (var item in list.Cast<ErrorMessageWindowViewModelVo>().ToArray())
        {
            ErrorMessage.Remove(item);
        }
    }

    [RelayCommand]
    private void SelectionChanged(IList selectedItems)
    {
        _selectedItems = selectedItems.Cast<ErrorMessageWindowViewModelVo>().ToArray();
        OnPropertyChanged(nameof(DeleteMenuItemHeader));
    }

    public sealed class ErrorMessageWindowViewModelVo : ErrorMessage
    {
        public string CodeDescription { get; }
        public ErrorMessageWindowViewModelVo(ErrorMessage message) 
            : base(message.Code, message.FileInfo, message.Message, message.Level)
        {
            CodeDescription = message.Code.Humanize();
        }
    }
}
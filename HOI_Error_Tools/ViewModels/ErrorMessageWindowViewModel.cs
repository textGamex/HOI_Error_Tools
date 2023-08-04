using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.View;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    public IReadOnlyList<ErrorMessage> ErrorMessage { get; }
    public string StatisticsInfo { get; }

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public ErrorMessageWindowViewModel(IReadOnlyList<ErrorMessage> errors)
    {
        ErrorMessage = errors;
        StatisticsInfo = $"错误 {ErrorMessage.Count}";
    }

    [RelayCommand]
    private static void OpenFolder(IList folderPathList)
    {
        foreach (ErrorMessage errorMessage in folderPathList)
        {
            _ = Process.Start("Explorer.exe", $"/select, {errorMessage.FileInfo.First().Item1}");
        }
    }

    [RelayCommand]
    private static void ShowErrorFileInfo(object obj)
    {
        var errorFileInfoWindow = new ErrorFileInfoView((IEnumerable<(string, Position)>)obj);
        errorFileInfoWindow.Show();
    }
}
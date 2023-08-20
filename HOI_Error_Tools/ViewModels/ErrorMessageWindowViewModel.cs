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
using Microsoft.Extensions.DependencyInjection;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    public IReadOnlyList<ErrorMessage> ErrorMessage { get; }
    public string StatisticsInfo { get; }

    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public ErrorMessageWindowViewModel(IReadOnlyList<ErrorMessage> errors)
    {
        var settings = App.Current.Services.GetRequiredService<GlobalSettings>();
        var rawCount = errors.Count;
        ErrorMessage = errors.Where(item => !settings.InhibitedErrorCodes.Contains(item.Code)).ToList();
        StatisticsInfo = $"错误 {ErrorMessage.Count}, 忽略 {rawCount - ErrorMessage.Count}";
    }

    //[RelayCommand]
    //private static void OpenFolder(IList folderPathList)
    //{
    //    foreach (ErrorMessage errorMessage in folderPathList)
    //    {
    //        _ = Process.Start("Explorer.exe", $"/select, {errorMessage.FileInfo.First().FilePath}");
    //    }
    //}

    [RelayCommand]
    private static void ShowErrorFileInfo(IEnumerable<ParameterFileInfo> obj)
    {
        Log.Debug("ErrorFileInfoView window start");
        var errorFileInfoWindow = new ErrorFileInfoView(obj);
        errorFileInfoWindow.Show();
    }
}

public class FilePathToErrorSourceTypeConverter : IValueConverter
{
    public static FilePathToErrorSourceTypeConverter Instance { get; } = new();
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable<ParameterFileInfo> fileInfo)
        {
            return fileInfo.First().FilePath.Contains("Hearts of Iron IV") ? "游戏" : "Mod";
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
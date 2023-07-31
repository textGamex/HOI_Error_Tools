using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.View;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private IImmutableList<ErrorMessage> _errorMessage;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public ErrorMessageWindowViewModel(IImmutableList<ErrorMessage> errors)
    {
        _errorMessage = errors;
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
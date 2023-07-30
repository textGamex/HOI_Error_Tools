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
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorMessageWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private IImmutableList<ErrorMessage> _errorMessage;

    private ErrorMessage? _selectErrorMessage;
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
            _ = Process.Start("Explorer.exe", $"/select, {errorMessage.FileInfo.First()}");
        }
    }

    [RelayCommand]
    private void SelectItemChange(object obj)
    {
        if (obj is ErrorMessage errorMessage)
        {
            _selectErrorMessage = errorMessage;
        }
        else
        {
            throw new ArgumentException();
        }
    }

    /*[RelayCommand]
    private void OpenFileInVsCode()
    {
        if (_selectErrorMessage is null)
        {
            _logger.Warn("{Name} 是 null", nameof(_selectErrorMessage));
            return;
        }

        var p = new Process();
        p.StartInfo.FileName = "cmd.exe";
        p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
        p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
        p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
        p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
        p.StartInfo.CreateNoWindow = true;//不显示程序窗口
        p.Start();//启动程序

        //向cmd窗口发送输入信息
        p.StandardInput.WriteLine($"code {_selectErrorMessage.FileInfo.First()}" + "&exit");
        p.StandardInput.AutoFlush = true;

        p.WaitForExit();//等待程序执行完退出进程
        p.Close();
    }*/
}
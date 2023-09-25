using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Services;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorFileInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private List<FileInfoVO> _data;

    private readonly IMessageBox _messageBox;
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public ErrorFileInfoViewModel(IErrorFileInfoService errorFileInfoService, IMessageBox messageBox)
    {
        _messageBox = messageBox;
        var errorInfoList = errorFileInfoService.GetFileErrorInfoList();
        _data = new List<FileInfoVO>(errorInfoList.Count);
        foreach (var fileInfo in errorInfoList)
        {
            Data.Add(new FileInfoVO(fileInfo.FilePath, fileInfo.Position.Line));
        }
    }

    [RelayCommand]
    private static void OpenFolder(FileInfoVO fileInfo)
    {
        _ = Process.Start("Explorer.exe", $"/select, {fileInfo.FilePath}");
    }

    [RelayCommand]
    private void OpenFileInVsCode(FileInfoVO fileInfo)
    {
        try
        {
            // see https://code.visualstudio.com/docs/editor/command-line
            var info = new ProcessStartInfo()
            {
                FileName = "code",
                Arguments = $"-g \"{fileInfo.FilePath}:{fileInfo.ErrorLine}:0\"",
                UseShellExecute = true,
            };
            var process = Process.Start(info);
            process?.Close();
        }
        catch (Exception e)
        {
            _messageBox.ErrorTip("VS Code 启动失败, 请检查您的电脑是否安装了 VS Code");
            Log.Warn(e, "VS Code 启动失败");
        }
    }

    public record FileInfoVO(string FilePath, long ErrorLine);
}
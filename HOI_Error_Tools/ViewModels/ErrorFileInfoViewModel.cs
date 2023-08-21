using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using HOI_Error_Tools.Services;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorFileInfoViewModel : ObservableObject
{
    [ObservableProperty]
    private List<FileInfoVO> _data;

    public ErrorFileInfoViewModel(IErrorFileInfoService errorFileInfoService)
    {
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

    public record FileInfoVO(string FilePath, long ErrorLine);
}
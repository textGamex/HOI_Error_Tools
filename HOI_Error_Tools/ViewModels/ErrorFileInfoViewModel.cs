using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorFileInfoViewModel : ObservableObject
{
    [ObservableProperty] 
    private List<FileInfoVO> _data;

    public ErrorFileInfoViewModel(IEnumerable<(string, Position)> fileInfo)
    {
        _data = new List<FileInfoVO>(8);
        foreach (var (path, position) in fileInfo)
        {
            Data.Add(new FileInfoVO(path, position.Line));
        }
    }

    public record FileInfoVO (string FilePath, long ErrorLine);
}
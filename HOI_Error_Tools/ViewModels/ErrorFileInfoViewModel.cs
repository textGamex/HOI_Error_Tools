using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.ViewModels;

public partial class ErrorFileInfoViewModel : ObservableObject
{
    [ObservableProperty] 
    private List<FileInfoVO> _data;

    public ErrorFileInfoViewModel(IEnumerable<ParameterFileInfo> fileInfoEnumerable)
    {
        _data = new List<FileInfoVO>(8);
        foreach (var fileInfo in fileInfoEnumerable)
        {
            Data.Add(new FileInfoVO(fileInfo.FilePath, fileInfo.Position.Line));
        }
    }

    public record FileInfoVO (string FilePath, long ErrorLine);
}
using System;
using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Services;

public class ErrorFileInfoService : IErrorFileInfoService
{
    private IReadOnlyList<ParameterFileInfo>? _fileErrorInfoList;
    public IReadOnlyList<ParameterFileInfo> GetFileErrorInfoList()
    {
        return _fileErrorInfoList ?? throw new InvalidOperationException($"{nameof(_fileErrorInfoList)} is null");
    }

    public void SetFileErrorInfoList(IReadOnlyList<ParameterFileInfo> fileErrorInfoList)
    {
        _fileErrorInfoList = fileErrorInfoList;
    }

    public void Clear()
    {
        _fileErrorInfoList = null;
    }
}
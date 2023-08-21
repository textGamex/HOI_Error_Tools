using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Services;

public interface IErrorFileInfoService
{
    IReadOnlyList<ParameterFileInfo> GetFileErrorInfoList();
    void SetFileErrorInfoList(IReadOnlyList<ParameterFileInfo> fileErrorInfoList);
    void Clear();
}
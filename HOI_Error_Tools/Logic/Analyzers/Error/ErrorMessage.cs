using HOI_Error_Tools.Logic.Analyzers.Common;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public partial class ErrorMessage
{
    public ErrorCode Code { get; }
    public IEnumerable<ParameterFileInfo> FileInfo { get; }
    public string Message { get; }
    public ErrorLevel Level { get; }

    public ErrorMessage(ErrorCode code, IEnumerable<ParameterFileInfo> fileInfo, string message, ErrorLevel level)
    {
        Code = code;
        FileInfo = fileInfo;
        Message = message;
        Level = level;
    }
}
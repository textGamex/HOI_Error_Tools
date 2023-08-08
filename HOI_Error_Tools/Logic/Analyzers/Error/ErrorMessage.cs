using HOI_Error_Tools.Logic.Analyzers.Common;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class ErrorMessage
{
    public IEnumerable<ParameterFileInfo> FileInfo { get; }
    public string Message { get; }
    public ErrorLevel Level { get; }

    public ErrorMessage(IEnumerable<ParameterFileInfo> fileInfo, string message, ErrorLevel level)
    {
        FileInfo = fileInfo;
        Message = message;
        Level = level;
    }
}
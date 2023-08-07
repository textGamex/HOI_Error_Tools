using System.Collections.Generic;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Common;

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

    public ErrorMessage(IEnumerable<(string, Position)> fileInfo, string message, ErrorLevel level)
        : this(fileInfo.Select(x => new ParameterFileInfo(x.Item1, x.Item2)), message, level)
    {
    }
}
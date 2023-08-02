using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class ErrorMessage
{
    public IEnumerable<(string, Position)> FileInfo { get; }
    public string Message { get; }   
    public ErrorLevel Level { get; }

    public ErrorMessage(IEnumerable<(string, Position)> fileInfo, string message, ErrorLevel level)
    {
        FileInfo = fileInfo;
        Message = message;
        Level = level;
    }
}
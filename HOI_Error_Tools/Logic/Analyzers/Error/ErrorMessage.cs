using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class ErrorMessage
{
    public IEnumerable<(string, Position)> FileInfo { get; }
    public string Message { get; }   
    public ErrorLevel Level { get; }

    public static ErrorMessage CreateSingleFileError(string filePath, string message, ErrorLevel level)
    {
        return new ErrorMessage(filePath, Position.Empty, message, level);
    }

    public static ErrorMessage CreateSingleFileErrorWithPosition(string filePath, Position position, string message, ErrorLevel level)
    {
        return new ErrorMessage(filePath, position, message, level);
    }

    private ErrorMessage(string filePath, Position position, string message, ErrorLevel level)
        : this(new[]{ (filePath, position) }, message, level)
    {
    }

    public ErrorMessage(IEnumerable<(string, Position)> fileInfo, string message, ErrorLevel level)
    {
        FileInfo = fileInfo;
        Message = message;
        Level = level;
    }
}
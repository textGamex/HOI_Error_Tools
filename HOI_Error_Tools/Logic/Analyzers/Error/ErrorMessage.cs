using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class ErrorMessage
{
    public IEnumerable<string> FilePaths => _filePathList;
    public string Message { get; }
    public ErrorType ErrorType { get; }
    public ErrorLevel Level { get; }
    public Position Position { get; }

    private readonly IEnumerable<string> _filePathList;

    public static ErrorMessage CreateSingleFileError(string filePath, string message, ErrorType errorType)
    {
        return new ErrorMessage(filePath, Position.Empty, message, errorType);
    }

    public static ErrorMessage CreateSingleFileErrorWithPosition(string filePath, Position position, string message,
        ErrorType errorType)
    {
        return new ErrorMessage(filePath, position, message, errorType);
    }

    private ErrorMessage(string filePath, Position position, string message, ErrorType errorType)
        : this(new[] { filePath }, position, message, errorType)
    {
    }

    public ErrorMessage(IEnumerable<string> filePaths, Position position, string message, ErrorType errorType)
        : this(filePaths, position, message, errorType, GetErrorLevelByType(errorType))
    { }

    public ErrorMessage(IEnumerable<string> filePaths, Position position, string message, ErrorType errorType, ErrorLevel level)
    {
        _filePathList = filePaths;
        Position = position;
        Message = message;
        ErrorType = errorType;
        Level = level;
    }

    private static ErrorLevel GetErrorLevelByType(ErrorType type)
    {
        return type switch
        {
            ErrorType.ParseError => ErrorLevel.Error,
            ErrorType.MissingKeyword => ErrorLevel.Error,
            ErrorType.None => ErrorLevel.None,
            _ => ErrorLevel.None,
        };
    }
}
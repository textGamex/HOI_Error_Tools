using System.IO;
using CWTools.CSharp;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public static class ErrorMessageFactory
{
    public static ErrorMessage CreateSingleFileError(string filePath, string message, ErrorLevel level)
    {
        return CreateSingleFileErrorWithPosition(filePath, Position.Empty, message, level);
    }

    public static ErrorMessage CreateSingleFileErrorWithPosition(string filePath, Position position, string message, ErrorLevel level)
    {
        return new ErrorMessage(new[] { (filePath, position) }, message, level);
    }

    public static ErrorMessage CreateParseErrorMessage(string filePath, ParserError error)
    {
        return CreateSingleFileErrorWithPosition(filePath, new Position(error), $"文件 '{Path.GetFileNameWithoutExtension(filePath)}' 解析错误", ErrorLevel.Error);
    }
}
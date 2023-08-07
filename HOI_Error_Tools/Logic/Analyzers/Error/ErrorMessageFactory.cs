using System.IO;
using CWTools.CSharp;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public static class ErrorMessageFactory
{
    public static ErrorMessage CreateSingleFileError(string filePath, string message, ErrorLevel level = ErrorLevel.Error)
    {
        return CreateSingleFileErrorWithPosition(filePath, Position.Empty, message, level);
    }

    public static ErrorMessage CreateSingleFileErrorWithPosition(string filePath, Position position, string message, ErrorLevel level = ErrorLevel.Error)
    {
        return new ErrorMessage(new[] { new ParameterFileInfo(filePath, position) }, message, level);
    }

    public static ErrorMessage CreateParseErrorMessage(string filePath, ParserError error)
    {
        return CreateSingleFileErrorWithPosition(filePath, new Position(error), $"文件 '{Path.GetFileNameWithoutExtension(filePath)}' 解析错误");
    }
}
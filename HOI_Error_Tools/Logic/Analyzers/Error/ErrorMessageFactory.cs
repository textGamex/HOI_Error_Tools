using CWTools.CSharp;
using HOI_Error_Tools.Logic.Analyzers.Common;
using System.IO;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public static class ErrorMessageFactory
{
    public static ErrorMessage CreateSingleFileError(ErrorCode code, string filePath, string message, ErrorLevel level = ErrorLevel.Error)
    {
        return CreateSingleFileErrorWithPosition(code, filePath, Position.Empty, message, level);
    }

    public static ErrorMessage CreateSingleFileErrorWithPosition(ErrorCode code, string filePath, Position position, string message, ErrorLevel level = ErrorLevel.Error)
    {
        return new ErrorMessage(code, new[] { new ParameterFileInfo(filePath, position) }, message, level);
    }

    public static ErrorMessage CreateParseErrorMessage(string filePath, ParserError error)
    {
        return CreateSingleFileErrorWithPosition(ErrorCode.ParseError, filePath, new Position(error), $"文件 '{Path.GetFileName(filePath)}' 解析错误");
    }

    public static ErrorMessage CreateFailedStringToIntErrorMessage(string filePath, LeafContent leaf)
    {
        return CreateSingleFileErrorWithPosition(ErrorCode.FailedStringToIntError, filePath, leaf.Position, $"{leaf.Key} '{leaf.ValueText}' 无法转换为整数");
    }

    public static ErrorMessage CreateInvalidValueErrorMessage(string filePath, LeafContent leaf, string expectedValueType)
    {
        return CreateSingleFileErrorWithPosition(ErrorCode.InvalidValue, filePath, leaf.Position, $"{leaf.Key}的值 '{leaf.ValueText}' 不是有效的, 应该是 '{expectedValueType}' 类型");
    }

    public static ErrorMessage CreateEmptyFileErrorMessage(string filePath)
    {
        return CreateSingleFileError(ErrorCode.EmptyFileError, filePath, $"文件 '{Path.GetFileName(filePath)}' 为空", ErrorLevel.Tip);
    }

    public static ErrorMessage CreateKeywordIsMissingErrorMessage(string filePath, LeavesNode node, string missingKeyword)
    {
        return CreateSingleFileErrorWithPosition(ErrorCode.KeywordIsMissing, filePath, node.Position, $"'{node.Key}' 缺少必需关键字 '{missingKeyword}'");
    }

    public static ErrorMessage CreateKeywordIsMissingErrorMessage(string filePath, string nodeKeyword, string missingKeyword)
    {
        return CreateSingleFileError(ErrorCode.KeywordIsMissing, filePath, $"'{nodeKeyword}' 缺少必需关键字 '{missingKeyword}'");
    }
}
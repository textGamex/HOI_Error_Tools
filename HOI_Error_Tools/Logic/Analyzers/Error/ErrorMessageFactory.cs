﻿using CWTools.CSharp;
using HOI_Error_Tools.Logic.Analyzers.Common;
using System.IO;

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

    public static ErrorMessage CreateFailedStringToIntErrorMessage(string filePath, LeafContent leaf)
    {
        return CreateSingleFileErrorWithPosition(filePath, leaf.Position, $"{leaf.Key} '{leaf.ValueText}' 无法转换为整数");
    }

    public static ErrorMessage CreateInvalidValueErrorMessage(string filePath, LeafContent leaf, string expectedValueType)
    {
        return CreateSingleFileErrorWithPosition(filePath, leaf.Position, $"{leaf.Key}的值 '{leaf.ValueText}' 不是有效的, 应该是 '{expectedValueType}' 类型");
    }
}
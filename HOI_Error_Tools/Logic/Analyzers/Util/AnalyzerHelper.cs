using System;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public sealed class AnalyzerHelper
{
    private readonly string _filePath;

    public AnalyzerHelper(string filePath)
    {
        _filePath = filePath;
    }

    public ErrorMessage? AssertExistKeyword<T>(IEnumerable<T> enumerable, string keyword, ErrorLevel level = ErrorLevel.Error)
    {
        return enumerable.Any()
            ? null
            : ErrorMessageFactory.CreateSingleFileError(_filePath, $"缺少 '{keyword}' 关键字", level);
    }

    /// <summary>
    /// 断言 <c>keyword</c> 只出现一次, 如果出现多次返回 <c>ErrorMessage</c>, 否则返回空集合
    /// </summary>
    /// <param name="enumerable"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    public IEnumerable<ErrorMessage> AssertKeywordIsOnly(IReadOnlyCollection<LeafContent> enumerable, string keyword)
    {
        return enumerable.Count > 1
            ? new[] { new ErrorMessage(enumerable.Select(item => new ParameterFileInfo(_filePath, item.Position)), $"重复的 '{keyword}' 关键字", ErrorLevel.Error) }
            : Enumerable.Empty<ErrorMessage>();
    }

    public IEnumerable<ErrorMessage> AssertKeywordsIsValid(LeavesNode node, IReadOnlySet<string> keywords)
    {
        foreach (var leaf in node.Leaves)
        {
            if (!keywords.Contains(leaf.Key))
            {
                yield return ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, leaf.Position, $"不应出现的关键字 '{leaf.Key}'");
            }
        }
    }


    public IEnumerable<ErrorMessage> AssertValueTypeIsExpected(LeavesNode node, IReadOnlyDictionary<string, Value.Types> map)
    {
        var errorList = new List<ErrorMessage>();

        foreach (var (keyword, valueType) in map)
        {
            var leafContent = node.Leaves.FirstOrDefault(leaf => leaf.Key == keyword);
            if (leafContent is not null && leafContent.Value.Type != valueType)
            {
                errorList.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                    _filePath, leafContent, Enum.GetName(valueType) ?? string.Empty));
            }
        }

        return errorList;
    }

    public IEnumerable<ErrorMessage> AssertValueTypeIsExpected(IEnumerable<LeafContent> leaves, Value.Types expectedType)
    {
        var list = new List<ErrorMessage>(3);
        foreach (var leaf in leaves)
        {
            if (leaf.Value.Type != expectedType)
            {
                 list.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                     _filePath, leaf, Enum.GetName(expectedType) ?? string.Empty));
            }
        }
        return list;
    }

    public IEnumerable<ErrorMessage> AssertValueTypeIsExpected(LeafContent leaf, Value.Types expectedType)
    {
        return leaf.Value.Type == expectedType
            ? Enumerable.Empty<ErrorMessage>()
            : new[] { ErrorMessageFactory.CreateInvalidValueErrorMessage(
                _filePath, leaf, Enum.GetName(expectedType) ?? string.Empty) };
    }
}
using System;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnumsNET;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public sealed class AnalyzerHelper
{
    private readonly string _filePath;
    private readonly string _fileName;

    public AnalyzerHelper(string filePath) 
        : this(filePath, Path.GetFileName(filePath))
    { }

    public AnalyzerHelper(string filePath, string fileName)
    {
        _filePath = filePath;
        _fileName = fileName;
    }

    public ErrorMessage? AssertExistKeyword<T>(IEnumerable<T> enumerable, string keyword, ErrorLevel level = ErrorLevel.Error)
    {
        return enumerable.Any()
            ? null
            : ErrorMessageFactory.CreateSingleFileError(ErrorCode.KeywordIsMissing,
                _filePath, $"文件 '{_fileName}' 缺少 '{keyword}' 关键字", level);
    }

    /// <summary>
    /// 断言 <c>keyword</c> 只出现一次, 如果出现多次返回 <see cref="ErrorMessage"/>, 如果未出现或者只出现一次, 返回空集合
    /// </summary>
    /// <param name="leaves"></param>
    /// <returns></returns>
    public IEnumerable<ErrorMessage> AssertKeywordIsOnly(IReadOnlyCollection<LeafContent> leaves)
    {
        return leaves.Count > 1
            ? new[]
            {
                new ErrorMessage(ErrorCode.KeywordIsRepeated,
                    leaves.Select(item => new ParameterFileInfo(_filePath, item.Position)),
                    $"文件 '{_fileName}' 中存在重复的 '{leaves.First().Key}' 关键字", ErrorLevel.Error)
            }
            : Enumerable.Empty<ErrorMessage>();
    }

    /// <summary>
    /// 仅允许每个不同的条件语句存在唯一一条相同指令.
    /// </summary>
    /// <param name="leaves"></param>
    /// <returns></returns>
    public IEnumerable<ErrorMessage> AssertKeywordIsOnly(IReadOnlyCollection<LeafContentWithCondition> leaves)
    {
        if (leaves.Count <= 1)
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var map = new Dictionary<Condition, LeafContentWithCondition>(leaves.Count);
        var errorList = new List<ErrorMessage>();
        foreach (var leaf in leaves)
        {
            var isRepeated = map.TryGetValue(leaf.Condition, out var oldLeaf);
            if (isRepeated)
            {
                var fileInfos = new ParameterFileInfo[]
                {
                    new (_filePath, leaf.Position),
                    new (_filePath, oldLeaf!.Position)
                };
                errorList.Add(new ErrorMessage(ErrorCode.UniqueValueIsRepeated, fileInfos, 
                    $"文件 {_fileName} 中语句 '{leaf.Key}' 重复", ErrorLevel.Error));
            }
            else
            {
                map.Add(leaf.Condition, leaf);
            }
        }

        return errorList;
    }

    public IEnumerable<ErrorMessage> AssertKeywordsIsValid(LeavesNode node, IReadOnlySet<string> keywords)
    {
        foreach (var leaf in node.Leaves)
        {
            if (!keywords.Contains(leaf.Key))
            {
                yield return ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, _filePath, leaf.Position, $"文件 '{_fileName}' 中存在不应出现的关键字 '{leaf.Key}'");
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
                    _filePath, leafContent, valueType.GetName() ?? string.Empty));
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
                     _filePath, leaf, expectedType.GetName() ?? string.Empty));
            }
        }
        return list;
    }

    public IEnumerable<ErrorMessage> AssertValueTypeIsExpected(LeafContent leaf, Value.Types expectedType)
    {
        return leaf.Value.Type == expectedType
            ? Enumerable.Empty<ErrorMessage>()
            : new[] { ErrorMessageFactory.CreateInvalidValueErrorMessage(
                _filePath, leaf, expectedType.GetName() ?? string.Empty) };
    }

    public IEnumerable<ErrorMessage> AssertValueIsOnly(IReadOnlyCollection<LeafContent> leaves, Func<string, string> repeatedValue,
        Func<LeafContent, string> valueSelector)
    {
        var map = new Dictionary<string, ParameterFileInfo>(leaves.Count);
        var errorList = new List<ErrorMessage>();
        foreach (var leaf in leaves)
        {
            var value = valueSelector(leaf);
            if (map.TryGetValue(value, out var parameterFileInfo))
            {
                var fileInfos = new[]
                {
                    new(_filePath, leaf.Position),
                    parameterFileInfo
                };
                errorList.Add(new ErrorMessage(ErrorCode.UniqueValueIsRepeated, fileInfos, repeatedValue(value),
                    ErrorLevel.Warn));
            }
            else
            {
                map[value] = new ParameterFileInfo(_filePath, leaf.Position);
            }
        }
        
        return errorList;
    }
}
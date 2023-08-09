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
    /// 断言 <c>keyword</c> 只出现一次, 如果出现多次或者未出现, 返回 <c>ErrorMessage</c>, 否则返回空集合
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
}
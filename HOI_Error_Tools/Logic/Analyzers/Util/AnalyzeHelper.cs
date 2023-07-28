using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using System.Collections.Immutable;
using HOI_Error_Tools.Logic.HOIParser;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public class AnalyzeHelper
{
    private readonly string _filePath;
    private readonly Node _rootNode;

    public AnalyzeHelper(string filePath, Node rootNode)
    {
        _filePath = filePath;
        _rootNode = rootNode;
    }

    /// <summary>
    /// 检查关键字在当前节点是否存在, 如果不存在, 返回 <see cref="ErrorMessage"/>
    /// </summary>
    /// <param name="keys">关键字</param>
    /// <returns><c>ErrorMessage</c></returns>
    public IEnumerable<ErrorMessage> AssertKeywordExistsInCurrentNode(params string[] keys)
    {
        return AssertKeywordExistsInCurrentNode(_rootNode, keys);
    }

    /// <summary>
    /// 检查关键字是否在传入节点的孩子中, 如果孩子不存在, 返回空集合
    /// </summary>
    /// <param name="childName">孩子名称</param>
    /// <param name="keys">需要检查的关键字</param>
    /// <returns></returns>
    public IEnumerable<ErrorMessage> AssertKeywordExistsInChild(string childName, params string[] keys)
    {
        if (_rootNode.HasNot(childName))
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var childNode = _rootNode.Child(childName).Value;

        return AssertKeywordExistsInCurrentNode(childNode, keys);
    }

    private IEnumerable<ErrorMessage> AssertKeywordExistsInCurrentNode(Node node, params string[] keys)
    {
        var errorMessageList = new List<ErrorMessage>(keys.Length);
        foreach (var key in keys)
        {
            if (node.HasNot(key))
            {
                errorMessageList.Add(ErrorMessage.CreateSingleFileError(_filePath, $"'{key}' 不存在", ErrorLevel.Error));
            }
        }
        
        return errorMessageList;
    }

    /// <summary>
    /// 检查关键字在当前节点是否存在, 如果不存在, 返回空集合. 此方法返回的<c>ErrorMessage</c>会附带被检测节点的位置
    /// </summary>
    /// <param name="keys"></param>
    /// <returns></returns>
    public IEnumerable<ErrorMessage> AssertKeywordExistsInCurrentNodeAndWithPosition(params string[] keys)
    {
        var errorMessageList = new List<ErrorMessage>(keys.Length);
        foreach (var key in keys)
        {
            if (_rootNode.HasNot(key))
            {
                errorMessageList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(_filePath, new Position(_rootNode.Position),$"'{key}' 不存在", ErrorLevel.Error));
            }
        }

        return errorMessageList;
    }
}
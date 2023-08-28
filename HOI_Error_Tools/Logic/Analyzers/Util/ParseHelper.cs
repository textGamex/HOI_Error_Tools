using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class ParseHelper
{
    /// <summary>
    /// 获得当前 Node 所有拥有指定 <c>leafKeyword</c> 的 <see cref="LeafContent"/>.
    /// </summary>
    /// <param name="leafKeyword"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetLeafContents(Node node, string leafKeyword)
    {
        return node.Leafs(leafKeyword)
            .Select(LeafContent.FromCWToolsLeaf);
    }

    /// <summary>
    /// 获得 <c>rootNode</c> 中所有拥有指定 <c>leafKeyword</c> 的 <see cref="LeafContent"/>. (包括 if else 语句和 Date 语句)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="leafKeyword"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetLeafContentsInAllChildren(Node rootNode, string leafKeyword)
    {
        return GetAllLeafInAllChildren(rootNode, leafKeyword)
            .Select(LeafContent.FromCWToolsLeaf);
    }

    /// <summary>
    /// 获得 <c>rootNode</c> 中所有拥有 <c>leafKeywords</c> 中的一个的 <see cref="LeafContent"/>. (包括 if else 语句和 Date 语句).
    /// </summary>
    /// <remarks>
    /// 如果要一次性获得多个不同 Key 的 LeafContent, 优先使用此方法, 而不是多次调用 <see cref="GetLeafContentsInAllChildren(Node, string)"/>,
    /// 此方法的性能优于多次调用 <see cref="GetLeafContentsInAllChildren(Node, string)"/>.
    /// </remarks>
    /// <param name="rootNode"></param>
    /// <param name="leafKeywords"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetLeafContentsInAllChildren(Node rootNode, IReadOnlySet<string> leafKeywords)
    {
        return GetAllLeafInAllChildren(rootNode, leafKeywords)
            .Select(LeafContent.FromCWToolsLeaf);
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有拥有指定 <c>leafKeyword</c> 的 <see cref="Leaf"/>. (包括 if 语句 和 Date 语句)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="leafKeyword"></param>
    /// <returns></returns>
    private static IEnumerable<Leaf> GetAllLeafInAllChildren(Node rootNode, string leafKeyword)
    {
        var nodeList = GetAllIfAndDateNode(rootNode);
        return nodeList.Append(rootNode).SelectMany(node => node.Leafs(leafKeyword));
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有拥有 <c>leafKeywords</c> 中的其中一个 keyword 的 <see cref="Leaf"/>. (包括 if 语句 和 Date 语句)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="leafKeywords"></param>
    /// <returns></returns>
    private static IEnumerable<Leaf> GetAllLeafInAllChildren(Node rootNode, IReadOnlySet<string> leafKeywords)
    {
        var nodes = GetAllIfAndDateNode(rootNode).Append(rootNode);

        var leafList = new List<Leaf>(16);
        foreach (var node in nodes)
        {
            leafList.AddRange(node.Leaves.Where(leaf => leafKeywords.Contains(leaf.Key)));
        }

        return leafList;
    }

    /// <summary>
    /// 获得当前 Node 所有 <see cref="LeafContent"/>.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetAllLeafContentInCurrentNode(Node node)
    {
        return node.Leaves
            .Select(LeafContent.FromCWToolsLeaf);
    }

    /// <summary>
    /// 获得 <c>rootNode</c> 中所有指定 Node 中的所有 <see cref="LeafContent"/>, 包括 If 和 日期语句下的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="nodeKey"></param>
    /// <returns></returns>
    public static IEnumerable<LeavesNode> GetAllLeafContentInRootNode(Node rootNode, string nodeKey)
    {
        var nodeList = GetAllEligibleNodeInAll(rootNode, nodeKey);
        return nodeList
            .Select(node => new LeavesNode(node.Key, GetAllLeafContentInCurrentNode(node), new Position(node.Position)))
            .ToList();
    }

    /// <summary>
    /// 获得所有 key 是 <c>keyword</c> 的 Node 的所有 <see cref="LeafValueContent"/>, 包括 If 语句中的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    public static IEnumerable<LeafValueNode> GetLeafValueNodesInAllNode(Node rootNode, string keyword)
    {
        var list = new List<LeafValueNode>(16);
        var nodeList = GetAllEligibleNodeInAll(rootNode, keyword);
        foreach (var node in nodeList)
        {
            if (!node.LeafValues.Any())
            {
                continue;
            }
            list.Add(new LeafValueNode(
                node.Key,
                node.LeafValues.Select(LeafValueContent.FromCWToolsLeafValue),
                new Position(node.Position)));
        }
        return list;
    }

    public static IEnumerable<LeafValueNode> GetLeafValueNodesInAllNode(Node rootNode, IReadOnlySet<string> keywordSet)
    {
        var list = new List<LeafValueNode>(16);
        var nodeList = GetAllEligibleNodeInAll(rootNode, keywordSet);
        foreach (var node in nodeList)
        {
            if (!node.LeafValues.Any())
            {
                continue;
            }
            list.Add(new LeafValueNode(
                node.Key,
                node.LeafValues.Select(LeafValueContent.FromCWToolsLeafValue),
                new Position(node.Position)));
        }
        return list;
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有 <c>Key</c> 是 <c>keyword</c> 的 <see cref="Node"/>. (包括在 if 语句和日期语句下的 Node).
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    private static IEnumerable<Node> GetAllEligibleNodeInAll(Node rootNode, string keyword)
    {
        var nodeList = GetAllIfAndDateNode(rootNode);

        return nodeList
            .SelectMany(node => node.Childs(keyword))
            .Concat(rootNode.Childs(keyword));
    }

    private static IEnumerable<Node> GetAllEligibleNodeInAll(Node rootNode, IReadOnlySet<string> keywordSet)
    {
        var nodeList = GetAllIfAndDateNode(rootNode).Append(rootNode);
        return nodeList.SelectMany(node => node.Nodes.Where(n => keywordSet.Contains(n.Key)));
    }

    private static IList<Node> GetAllIfAndDateNode(Node rootNode)
    {
        var nodeList = new List<Node>(8);

        foreach (var node in rootNode.Nodes)
        {
            if (node.Key == "if")
            {
                nodeList.Add(node);
                
                if (node.Has("else"))
                {
                    nodeList.Add(node.Child("else").Value);
                }
            }
            else if (Value.IsDateString(node.Key))
            {
                nodeList.Add(node);
                // 使用递归是为了读取嵌套在 Date 语句下的 If 语句.
                nodeList.AddRange(GetAllIfAndDateNode(node));
            }
        }
        return nodeList;
    }

    /// <summary>
    /// 解析文件, 如果解析失败, 将错误信息添加到 <c>errorMessages</c>, 返回 <c>null</c>
    /// </summary>
    /// <param name="errorMessages">错误信息集合</param>
    /// <param name="filePath">文件绝对路径</param>
    /// <returns>root Node</returns>
    public static Node? ParseFileToNode(ICollection<ErrorMessage> errorMessages, string filePath)
    {
        var parser = new CWToolsParser(filePath);
        if (parser.IsSuccess)
        {
            return parser.GetResult();
        }
        errorMessages.Add(ErrorMessageFactory.CreateParseErrorMessage(filePath, parser.GetError()));
        return null;
    }


    /// <inheritdoc cref="ParseFileToNode(ICollection{ErrorMessage},string)"/>
    public static Node? ParseFileToNode(IProducerConsumerCollection<ErrorMessage> errorMessages, string filePath)
    {
        var parser = new CWToolsParser(filePath);
        if (parser.IsSuccess)
        {
            return parser.GetResult();
        }
        var result = errorMessages.TryAdd(ErrorMessageFactory.CreateParseErrorMessage(filePath, parser.GetError()));
        Debug.Assert(result);
        return null;
    }
}
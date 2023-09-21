using System;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public static IEnumerable<LeafContent> GetLeafContentsInChildren(Node rootNode, string leafKeyword)
    {
        return GetAllLeafInAllChildren(rootNode, leafKeyword)
            .Select(LeafContent.FromCWToolsLeaf);
    }

    public static IEnumerable<LeafContentWithCondition> GetLeafContentsWithConditionInChildren(Node rootNode,
        string leafKeyword)
    {
        return GetValueWithCondition<LeafContentWithCondition, Leaf>(rootNode, node => node.Leafs(leafKeyword),
            (leaves, condition) => leaves.Select(leaf => LeafContentWithCondition.Create(leaf, condition)));
    }

    /// <summary>
    /// 获得 <c>rootNode</c> 中所有拥有 <c>leafKeywords</c> 中的一个的 <see cref="LeafContent"/>. (包括 if else 语句和 Date 语句).
    /// </summary>
    /// <remarks>
    /// 如果要一次性获得多个不同 <c>Key</c> 的 <c>LeafContent</c>, 优先使用此方法,
    /// 而不是多次调用 <see cref="GetLeafContentsInChildren(CWTools.Process.Node,string)"/>,
    /// 此方法的性能优于多次调用 <see cref="GetLeafContentsInChildren(CWTools.Process.Node,string)"/>.
    /// </remarks>
    /// <param name="rootNode"></param>
    /// <param name="leafKeywords"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetLeafContentsInChildren(Node rootNode, IReadOnlySet<string> leafKeywords)
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
        var nodeList = GetAllNodes(rootNode);
        return nodeList.SelectMany(node => node.Leafs(leafKeyword));
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有拥有 <c>leafKeywords</c> 中的其中一个 keyword 的 <see cref="Leaf"/>. (包括 if 语句 和 Date 语句)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="leafKeywords"></param>
    /// <returns></returns>
    private static IEnumerable<Leaf> GetAllLeafInAllChildren(Node rootNode, IReadOnlySet<string> leafKeywords)
    {
        var nodes = GetAllNodes(rootNode);
        var leafList = new List<Leaf>(8);
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
        var nodeList = GetEligibleNodeInAllNodes(rootNode, nodeKey);
        return nodeList
            .Select(node => 
                new LeavesNode(node.Key, GetAllLeafContentInCurrentNode(node), new Position(node.Position)))
            .ToList();
    }

    public static IEnumerable<LeavesNodeWithCondition> GetAllLeafContentWithConditionsInRootNode(Node rootNode,
        string nodeKey)
    {
        return GetValueWithCondition<LeavesNodeWithCondition, Node>(rootNode, node => node.Childs(nodeKey), 
            (nodes, condition) => nodes.Select(n => 
                new LeavesNodeWithCondition(n.Key, GetAllLeafContentInCurrentNode(n), new Position(n.Position), condition)));
    }

    private static IEnumerable<T> GetValueWithCondition<T,TU>(Node rootNode, Func<Node,IEnumerable<TU>> func, 
        Func<IEnumerable<TU>, Condition, IEnumerable<T>> action)
    {
        var list = new List<T>();
        foreach (var node in GetAllNodes(rootNode))
        {
            var condition = Condition.Empty;
            var enumerable = func(node);
            if (Value.TryParseDate(node.Key, out var date))
            {
                condition = new Condition(date);
            }
            list.AddRange(action(enumerable, condition));
        }
        return list;
    }

    /// <summary>
    /// 获得所有 key 是 <c>keyword</c> 的 Node 的所有 <see cref="LeafValueContent"/>, 包括 if/esle/Date 语句中的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    public static IEnumerable<LeafValueNode> GetLeafValueNodesInChildren(Node rootNode, string keyword)
    {
        return GetLeafValueNodesInChildren(() => GetEligibleNodeInAllNodes(rootNode, keyword));
    }

    public static IEnumerable<LeafValueNode> GetLeafValueNodesInChildren(Node rootNode, IReadOnlySet<string> keywordSet)
    {
        return GetLeafValueNodesInChildren(() => GetEligibleNodeInAllNodes(rootNode, keywordSet));
    }

    private static IEnumerable<LeafValueNode> GetLeafValueNodesInChildren(Func<IEnumerable<Node>> nodesSource)
    {
        var list = new List<LeafValueNode>(16);
        var nodeList = nodesSource();
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
    /// 获得在 <c>rootNode</c> 中所有 <c>Key</c> 是 <c>keyword</c> 的 <see cref="Node"/>. (包括在 if/else 语句和日期语句下的 Node).
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    private static IEnumerable<Node> GetEligibleNodeInAllNodes(Node rootNode, string keyword)
    {
        return GetEligibleNodeInAllNodes(rootNode, nodes =>
            nodes.SelectMany(node => node.Childs(keyword)));
    }

    private static IEnumerable<Node> GetEligibleNodeInAllNodes(Node rootNode, IReadOnlySet<string> keywordSet)
    {
        return GetEligibleNodeInAllNodes(rootNode, nodes =>
            nodes.SelectMany(node => node.Nodes.Where(n => keywordSet.Contains(n.Key))));
    }

    private static IEnumerable<Node> GetEligibleNodeInAllNodes(Node rootNode, Func<IEnumerable<Node>,
        IEnumerable<Node>> selector)
    {
        var nodeList = GetAllNodes(rootNode);
        return selector(nodeList);
    }

    /// <summary>
    /// 获得全部节点 (if, else, Date, rootNode)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <returns></returns>
    private static IEnumerable<Node> GetAllNodes(Node rootNode)
    {
        return GetAllIfAndDateNode(rootNode).Prepend(rootNode);
    }
    
    /// <summary>
    /// 获得 <c>rootNode</c> 下的 if/else/date 节点, 不包含 <c>rootNode</c>.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <returns><c>rootNode</c> 下的 if/else/date 节点列表, 不包含 <c>rootNode</c></returns>
    private static IList<Node> GetAllIfAndDateNode(Node rootNode)
    {
        var nodeList = new List<Node>(8);

        foreach (var node in rootNode.Nodes)
        {
            if (node.Key.Equals("if", StringComparison.OrdinalIgnoreCase))
            {
                nodeList.Add(node);
                AddElseNodesToList(node);
            }
            else if (Value.IsDateString(node.Key))
            {
                nodeList.Add(node);
                // 使用递归是为了读取嵌套在 Date 语句下的 If 语句.
                nodeList.AddRange(GetAllIfAndDateNode(node));
            }
        }
        return nodeList;

        // 虽然说一个 if 里只面允许存在一个 else, 但谁知道到底有几个.
        void AddElseNodesToList(Node n)
        {
            nodeList.AddRange(n.Nodes.Where(childNode => 
                childNode.Key.Equals("else", StringComparison.OrdinalIgnoreCase)));
        }
    }

    public static Dictionary<string, List<LeafContent>> GetLeafContentsByKeywordsInChildren(Node rootNode,
        IReadOnlySet<string> keywords)
    {
        var map = new Dictionary<string, List<LeafContent>>(keywords.Count);
        foreach (var keyword in keywords)
        {
            map[keyword] = new List<LeafContent>();
        }
        
        var leaves = GetLeafContentsInChildren(rootNode, keywords);
        foreach (var leaf in leaves)
        {
            map[leaf.Key].Add(leaf);
        }
        
        return map;
    }

    #region 文件解析相关

    /// <summary>
    /// 解析文件, 如果解析失败, 将错误信息添加到 <c>errorMessages</c>, 返回 <c>null</c>
    /// </summary>
    /// <param name="errorMessages">错误信息集合</param>
    /// <param name="filePath">文件绝对路径</param>
    /// <exception cref="IOException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    /// <returns>root Node</returns>
    public static Node? ParseFileToNode(ICollection<ErrorMessage> errorMessages, string filePath)
    {
        return ParseFileToNode(filePath, errorMessages.Add);
    }


    /// <inheritdoc cref="ParseFileToNode(ICollection{ErrorMessage},string)"/>
    public static Node? ParseFileToNode(IProducerConsumerCollection<ErrorMessage> errorMessages, string filePath)
    {
        return ParseFileToNode(filePath, message =>
        {
            var result = errorMessages.TryAdd(message);
            Debug.Assert(result);
        });
    }

    private static Node? ParseFileToNode(string filePath, Action<ErrorMessage> errorMessage)
    {
        var parser = new CWToolsParser(filePath);
        if (parser.IsSuccess)
        {
            return parser.GetResult();
        }
        errorMessage(ErrorMessageFactory.CreateParseErrorMessage(filePath, parser.GetError()));
        return null;
    }
    #endregion
}
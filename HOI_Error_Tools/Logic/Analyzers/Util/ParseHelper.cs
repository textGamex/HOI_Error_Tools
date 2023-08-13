using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
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
    /// 获得 <c>rootNode</c> 中所有拥有指定 <c>leafKeyword</c> 的 <see cref="LeafContent"/>. (包括 if else 语句)
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
    /// 获得 <c>rootNode</c> 中所有指定 Node 的所有 <see cref="LeafContent"/>, 包括 If 语句中的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="nodeKey"></param>
    /// <returns></returns>
    public static IEnumerable<LeavesNode> GetAllLeafKeyAndValueInAllNode(Node rootNode, string nodeKey)
    {
        var nodeList = GetAllNodeInAll(rootNode, nodeKey);
        return nodeList
            .Select(node => new LeavesNode(GetAllLeafContentInCurrentNode(node), new Position(node.Position)))
            .ToList();
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有拥有指定 <c>keyword</c> 的 <see cref="Node"/>. (包括 if 语句).
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    private static IEnumerable<Node> GetAllNodeInAll(Node rootNode, string keyword)
    {
        var nodeList = new List<Node>(8);
        nodeList.AddRange(GetAllScriptNode(rootNode));
        nodeList.AddRange(GetAllIfAndElseNode(rootNode));
        return nodeList
            .Where(node => node.Has(keyword))
            .SelectMany(node => node.Childs(keyword))
            .Concat(rootNode.Childs(keyword));
    }

    /// <summary>
    /// 获得在 <c>rootNode</c> 中所有拥有指定 <c>leafKeyword</c> 的 <see cref="Leaf"/>. (包括 if 语句)
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="leafKeyword"></param>
    /// <returns></returns>
    private static IEnumerable<Leaf> GetAllLeafInAllChildren(Node rootNode, string leafKeyword)
    {
        var nodeList = GetAllIfAndElseNode(rootNode);
        return nodeList.Append(rootNode).SelectMany(node => node.Leafs(leafKeyword));
    }

    /// <summary>
    /// 获得 <c>rootNode</c> 中所有的 If Node 和 Else Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <returns>所有的 If Node 和 Else Node</returns>
    private static IEnumerable<Node> GetAllIfAndElseNode(Node rootNode)
    {
        var nodeList = new List<Node>();
        var ifStatement = rootNode.Childs("if");
        foreach (var node in ifStatement)
        {
            const string elseKey = "else";
            if (node.Has(elseKey))
            {
                var elseNode = node.Child(elseKey).Value;
                nodeList.Add(elseNode);
            }
            nodeList.Add(node);
        }
        return nodeList;
    }

    private static IReadOnlyList<Node> GetAllScriptNode(Node rootNode)
    {
        return rootNode.Nodes.Where(node => Value.IsDateString(node.Key)).ToList();
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
        var nodeList = GetAllNodeInAll(rootNode, keyword);
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
}
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
    /// 获得在 <c>rootNode</c> 中所有拥有指定 <c>leafKeyword</c> 的 <see cref="Leaf"/>. (包括 if 语句)
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

    private static IReadOnlyList<Node> GetAllIfAndDateNode(Node rootNode)
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
}
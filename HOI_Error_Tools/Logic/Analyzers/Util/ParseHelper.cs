using System.Collections.Generic;
using CWTools.Process;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class ParseHelper
{
    /// <summary>
    /// 获得当前 Node 所有指定 LeafContent 的 ValueText.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<(string, Position)> GetLeavesValue(string key, Node node)
    {
        return node.Leafs(key)
            .Select(x => (x.ValueText, new Position(x.Position)));
    }

    /// <summary>
    /// 获得当前 Node 所有 Leaf 的 Key 和 ValueText.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<LeafContent> GetLeavesKeyValuePairs(Node node)
    {
        return node.Leaves
            .Select(LeafContent.FromCWToolsLeaf);
    }

    /// <summary>
    /// 获得 root Node 中所有指定 Node 的所有 Leaf 的 Key 和 ValueText, 包括 If 语句中的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="nodeKey"></param>
    /// <returns></returns>
    public static IEnumerable<(IEnumerable<LeafContent> NodeContent, Position)> GetAllLeafKeyAndValueInAllNode(Node rootNode, string nodeKey)
    {
        //if (rootNode.HasNot(nodeKey))
        //{
        //    return Enumerable.Empty<(IEnumerable<LeafContent> NodeContent, Position)>();
        //}

        var nodeList = new List<Node>();
        nodeList.AddRange(rootNode.Childs(nodeKey));
        nodeList.AddRange(GetAllNodeInIfStatement(rootNode, nodeKey));
        return nodeList.Select(node => (GetLeavesKeyValuePairs(node), new Position(node.Position))).ToList();
    }

    /// <summary>
    /// 根据<c>keyword</c>获得 if 语句中的所有符合条件的 Node.
    /// </summary>
    /// <param name="rootNode"></param>
    /// <param name="keyword"></param>
    /// <returns></returns>
    private static IEnumerable<Node> GetAllNodeInIfStatement(Node rootNode, string keyword)
    {
        var list = new List<Node>();
        var ifStatement = rootNode.Childs("if");
        foreach (var node in ifStatement)
        {
            if (node.Has("else"))
            {
                var elseNode = node.Child("else").Value;
                list.Add(elseNode);
            }
            list.Add(node);
        }

        var result = new List<Node>();
        foreach (var nodes in list.Where(node => node.Has(keyword)).Select(node => node.Childs(keyword)))
        {
            result.AddRange(nodes);
        }
        return result;
    }
}
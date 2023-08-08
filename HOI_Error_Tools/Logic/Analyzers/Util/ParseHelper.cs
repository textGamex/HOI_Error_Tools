﻿using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class ParseHelper
{
    /// <summary>
    /// 获得当前 Node 所有指定 LeafContent 的 ValueText.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="node"></param>
    /// <returns></returns>
    public static IEnumerable<(string ValueOfLeaf, Position)> GetLeavesValue(string key, Node node)
    {
        return node.Leafs(key)
            .Select(leaf => (leaf.ValueText, new Position(leaf.Position)));
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
    public static IEnumerable<LeavesNode> GetAllLeafKeyAndValueInAllNode(Node rootNode, string nodeKey)
    {
        var nodeList = GetAllNodeInAll(rootNode, nodeKey);
        return nodeList.Select(node => new LeavesNode(GetLeavesKeyValuePairs(node), new Position(node.Position))).ToList();
    }

    private static IEnumerable<Node> GetAllNodeInAll(Node rootNode, string keyword)
    {
        var nodeList = new List<Node>();
        nodeList.AddRange(rootNode.Childs(keyword));
        nodeList.AddRange(GetAllNodeInIfStatement(rootNode, keyword));
        return nodeList;
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
            list.Add(new LeafValueNode(node.LeafValues.Select(LeafValueContent.FromCWToolsLeafValue), new Position(node.Position)));
        }
        return list;
    }
}
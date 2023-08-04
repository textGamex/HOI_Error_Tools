using System.Collections.Generic;
using CWTools.Process;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class ParseHelper
{
    /// <summary>
    /// 获得当前 Node 所有指定 Leaf 的 ValueText.
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
    public static IEnumerable<(string, string, Position)> GetLeavesKeyValuePairs(Node node)
    {
        return node.Leaves
            .Select(leaf => (leaf.Key, leaf.ValueText, new Position(leaf.Position)));
    }

    public static IEnumerable<(IEnumerable<(string Key, string Value, Position)> NodeContent, Position)> GetAllLeafKeyAndValueInAllNode(Node rootNode, string nodeKey)
    {
        if (rootNode.HasNot(nodeKey))
        {
            return Enumerable.Empty<(IEnumerable <(string Key, string Value, Position)> NodeContent, Position)>();
        }

        var nodes = rootNode.Childs(nodeKey);

        return nodes.Select(node => (GetLeavesKeyValuePairs(node), new Position(node.Position))).ToList();
    }
}
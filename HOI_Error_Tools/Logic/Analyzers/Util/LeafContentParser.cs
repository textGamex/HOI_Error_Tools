using System.Collections.Generic;
using System.Runtime.InteropServices;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public static class LeafContentParser
{
    public static void ParseValueToObject<T>(ParserTargetKeywords parserTargetKeywords, T obj, Node rootNode)
    {
        var targetKeywords = parserTargetKeywords.Keywords;
        var map = new Dictionary<string, List<LeafContent>>(targetKeywords.Count);
        foreach (var keywords in parserTargetKeywords.TargetKeywords.Values)
        {
            var sharedList = new List<LeafContent>();
            foreach (var keyword in CollectionsMarshal.AsSpan(keywords))
            {
                map[keyword] = sharedList;
            }
        }

        var leaves = ParseHelper.GetLeafContentsInChildren(rootNode, targetKeywords);
        foreach (var leaf in leaves)
        {
            map[leaf.Key].Add(leaf);
        }

        var result = new Dictionary<ParserTargetKeywords.KeywordGroupToken, List<LeafContent>>(parserTargetKeywords.TargetKeywords.Count);
        foreach (var item in parserTargetKeywords.TargetKeywords.Keys)
        {
            var keywordsList = parserTargetKeywords.TargetKeywords[item];
            result.Add(item, map[keywordsList[0]]);
        }

        foreach (var (propertyName, value) in parserTargetKeywords.ObjectPropertiesSetter)
        {
            typeof(T).GetProperty(propertyName)?.SetValue(obj, result[value]);
        }
    }
}
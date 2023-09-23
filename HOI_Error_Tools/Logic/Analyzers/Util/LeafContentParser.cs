using System.Collections.Generic;
using System.Runtime.InteropServices;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public class LeafContentParser
{
    public LeafContentParser(ParserTargetKeywords targetKeywords)
    {
        _targetKeywords = targetKeywords;
    }

    public Dictionary<ParserTargetKeywords.KeywordGroupToken, List<LeafContent>> Parse(Node rootNode)
    {
        var targetKeywords = _targetKeywords.Keywords;
        var map = new Dictionary<string, List<LeafContent>>(targetKeywords.Count);
        foreach (var keywords in _targetKeywords.TargetKeywords.Values)
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
        
        var result = new Dictionary<ParserTargetKeywords.KeywordGroupToken, List<LeafContent>>(_targetKeywords.TargetKeywords.Count);
        foreach (var item in _targetKeywords.TargetKeywords.Keys)
        {
            var keywordsList = _targetKeywords.TargetKeywords[item];
            result.Add(item, map[keywordsList[0]]);
        }

        return result;
    }

    private readonly ParserTargetKeywords _targetKeywords;
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer
{
    public sealed class CountryDefineFileModel
    {
        public IReadOnlyList<LeavesNode> SetPopularitiesList { get; }
        public IReadOnlyList<LeafValueNode> OwnIdeaNodes { get; }
        public IReadOnlyList<LeafContent> OwnIdeaLeaves { get; }
        public IReadOnlyList<LeavesNode> SetPoliticsList { get; }
        public IReadOnlyList<LeafContent> Capitals { get; }
        public IReadOnlyList<LeafContent> Puppets { get; }
        public IReadOnlyList<LeafContent> CountriesTagOfAddToFaction { get; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; }
        public IReadOnlyList<LeafContent> GiveGuaranteeCountriesTag { get; }
        public IReadOnlyList<LeafContent> OwnCharacters { get; }
        public IReadOnlyList<LeafContent> OwnOobs { get; }
        
        private readonly Node _rootNode;

        private static readonly WeakReference<ImmutableHashSet<string>?> OwnOobsKeywords = new(null);
        private static readonly WeakReference<ImmutableHashSet<string>?> OwnCharactersKeywords = new(null);
        private static readonly WeakReference<ImmutableHashSet<string>?> OwnIdeasKeywords = new(null);

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            var keywords = new HashSet<string>(5)
            {
                Keywords.AddIdea,
                Keywords.Capital,
                Keywords.Puppet,
                Keywords.GiveGuarantee,
                Keywords.AddToFaction
            };
            var leaves = ParseHelper.GetLeafContentsByKeywordsInChildren(rootNode, keywords);
            OwnIdeaLeaves = leaves[Keywords.AddIdea];
            Capitals = leaves[Keywords.Capital];
            Puppets = leaves[Keywords.Puppet];
            CountriesTagOfAddToFaction = leaves[Keywords.AddToFaction];
            GiveGuaranteeCountriesTag = leaves[Keywords.GiveGuarantee];
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
            OwnCharacters = GetOwnCharacters();
            OwnOobs = GetOwnOobs();
        }
        
        private IReadOnlyList<LeafContent> GetOwnOobs()
        {
            return GetValueFromWeakReference(OwnOobsKeywords, 
                () => new[] {"oob", "set_oob", "set_naval_oob", "set_air_oob"},
                keywords => ParseHelper.GetLeafContentsInChildren(_rootNode, keywords).ToList());
        }

        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            return GetValueFromWeakReference(OwnIdeasKeywords, () => new[] { "add_ideas", "remove_ideas" }, 
                keywords => ParseHelper.GetLeafValueNodesInChildren(_rootNode, keywords).ToList());
        }
        
        private IReadOnlyList<LeafContent> GetOwnCharacters()
        {
            return GetValueFromWeakReference(OwnCharactersKeywords, 
                () => new[] { "recruit_character", "promote_character", "retire_character" }, 
                keywords => ParseHelper.GetLeafContentsInChildren(_rootNode, keywords).ToList());
        }
        
        private static IReadOnlyList<T> GetValueFromWeakReference<T>(
            WeakReference<ImmutableHashSet<string>?> weakReference, Func<IEnumerable<string>> keywords, 
            Func<IReadOnlySet<string>, IReadOnlyList<T>> parseResult)
        {
            if (!weakReference.TryGetTarget(out var keys))
            {
                keys = ImmutableHashSet.CreateRange(keywords());
                weakReference.SetTarget(keys);
            }
            return parseResult(keys);
        }
        
        private static class Keywords
        {
            public const string AddIdea = "add_ideas";
            public const string Capital = "capital";
            public const string Puppet = "puppet";
            public const string GiveGuarantee = "give_guarantee";
            public const string AddToFaction = "add_to_faction";
        }
    }
}
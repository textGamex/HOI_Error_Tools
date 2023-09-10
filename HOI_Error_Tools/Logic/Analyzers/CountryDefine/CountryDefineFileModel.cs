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
            if (!OwnOobsKeywords.TryGetTarget(out var keywords))
            {
                keywords = ImmutableHashSet.CreateRange(new[] { "oob", "set_oob", "set_naval_oob", "set_air_oob" });
                OwnOobsKeywords.SetTarget(keywords);
            }
            return ParseHelper.GetLeafContentsInChildren(_rootNode, keywords)
                .ToList();
        }

        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            if (!OwnIdeasKeywords.TryGetTarget(out var keywords))
            {
                keywords = ImmutableHashSet.CreateRange(new[] { "add_ideas", "remove_ideas" });
                OwnIdeasKeywords.SetTarget(keywords);
            }
            return ParseHelper.GetLeafValueNodesInChildren(_rootNode, keywords)
                .ToList();
        }
        
        private IReadOnlyList<LeafContent> GetOwnCharacters()
        {
            if (!OwnCharactersKeywords.TryGetTarget(out var keywords))
            {
                keywords = ImmutableHashSet.CreateRange(new[] { "recruit_character", "promote_character", "retire_character" });
                OwnCharactersKeywords.SetTarget(keywords);
            }
            return ParseHelper.GetLeafContentsInChildren(_rootNode, keywords)
                .ToList();
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
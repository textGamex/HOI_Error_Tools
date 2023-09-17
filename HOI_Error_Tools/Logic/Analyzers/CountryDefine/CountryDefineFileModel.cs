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
        public IReadOnlyList<LeafContent> UsedCountryTags { get; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; }
        public IReadOnlyList<LeafContent> OwnCharacters { get; }
        public IReadOnlyList<LeafContent> OwnOobs { get; }
        
        private readonly Node _rootNode;

        private static readonly ImmutableHashSet<string> OwnOobsKeywords = 
            ImmutableHashSet.CreateRange(new []{"oob", "set_oob", "set_naval_oob", "set_air_oob"});
        private static readonly ImmutableHashSet<string> OwnCharactersKeywords = 
            ImmutableHashSet.CreateRange(new[] { "recruit_character", "promote_character", "retire_character" });
        private static readonly ImmutableHashSet<string> OwnIdeasKeywords = 
            ImmutableHashSet.CreateRange(new []{Keywords.AddIdeas, Keywords.RemoveIdeas});

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            var target = new ParserTargetKeywords();
            var ideaLeavesToken = target.Add(Keywords.AddIdeas, Keywords.RemoveIdeas);
            var capitalsToken = target.Add(Keywords.Capital);
            var usedCountryTagsToken = target.Add(Keywords.Puppet, Keywords.EndPuppet, Keywords.AddToFaction,
                Keywords.GiveGuarantee, "remove_core_of", "remove_claim_by", "release_puppet", "release",
                "give_military_access");
            var leafParserResult = new LeafContentParser(target).Parse(rootNode);
            OwnIdeaLeaves = leafParserResult[ideaLeavesToken];
            Capitals = leafParserResult[capitalsToken];
            UsedCountryTags = leafParserResult[usedCountryTagsToken];
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
            OwnCharacters = GetOwnCharacters();
            OwnOobs = GetOwnOobs();
        }
        
        private IReadOnlyList<LeafContent> GetOwnOobs()
        {
            return ParseHelper.GetLeafContentsInChildren(_rootNode, OwnOobsKeywords).ToList();
        }

        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            return ParseHelper.GetLeafValueNodesInChildren(_rootNode, OwnIdeasKeywords).ToList();
        }
        
        private IReadOnlyList<LeafContent> GetOwnCharacters()
        {
            return ParseHelper.GetLeafContentsInChildren(_rootNode, OwnCharactersKeywords).ToList();
        }
        
        private static class Keywords
        {
            public const string AddIdeas = "add_ideas";
            public const string Capital = "capital";
            public const string Puppet = "puppet";
            public const string EndPuppet = "end_puppet";
            public const string GiveGuarantee = "give_guarantee";
            public const string AddToFaction = "add_to_faction";
            public const string RemoveIdeas = "remove_ideas";
        }
    }
}
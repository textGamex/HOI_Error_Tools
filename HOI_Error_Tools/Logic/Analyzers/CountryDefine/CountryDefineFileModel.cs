using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public sealed partial class CountryDefineFileAnalyzer
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
        public IReadOnlyList<LeavesNode> UsedVariable { get; }
        
        private readonly Node _rootNode;
        
        private static readonly ImmutableHashSet<string> OwnIdeasKeywords = 
            ImmutableHashSet.CreateRange(new []{Keywords.AddIdeas, Keywords.RemoveIdeas});

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            UsedVariable = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_variable").ToList();
            
            var target = new ParserTargetKeywords();
            var ideaLeavesToken = target.Add(Keywords.AddIdeas, Keywords.RemoveIdeas);
            var capitalsToken = target.Add(Keywords.Capital);
            var oobToken = target.Add("oob", "set_oob", "set_naval_oob", "set_air_oob", "load_oob");
            var charactersToken = target.Add("recruit_character", "promote_character", "retire_character");
            var usedCountryTagsToken = target.Add(Keywords.Puppet, Keywords.EndPuppet, Keywords.AddToFaction,
                Keywords.GiveGuarantee, "remove_core_of", "remove_claim_by", "release_puppet", "release",
                "give_military_access");
            var leafParserResult = new LeafContentParser(target).Parse(rootNode);
            OwnOobs = leafParserResult[oobToken];
            OwnIdeaLeaves = leafParserResult[ideaLeavesToken];
            Capitals = leafParserResult[capitalsToken];
            UsedCountryTags = leafParserResult[usedCountryTagsToken];
            OwnCharacters = leafParserResult[charactersToken];
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
        }
        
        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            return ParseHelper.GetLeafValueNodesInChildren(_rootNode, OwnIdeasKeywords).ToList();
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
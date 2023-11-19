using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public sealed partial class CountryDefineFileAnalyzer
{
    public sealed class CountryDefineFileModel
    {
        public IReadOnlyList<LeavesNode> SetPopularitiesList { get; private set; }
        public IReadOnlyList<LeafValueNode> OwnIdeaNodes { get; private set; }
        public IReadOnlyList<LeafContent> OwnIdeaLeaves { get; private set; }
        public IReadOnlyList<LeavesNode> SetPoliticsList { get; private set; }
        public IReadOnlyList<LeafContent> Capitals { get; private set; }
        public IReadOnlyList<LeafContent> UsedCountryTags { get; private set; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; private set; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; private set; }
        public IReadOnlyList<LeafContent> OwnCharacters { get; private set; }
        public IReadOnlyList<LeafContent> OwnOobs { get; private set; }
        public IReadOnlyList<LeavesNode> UsedVariable { get; private set; }
        
        private readonly Node _rootNode;
        
        private static readonly FrozenSet<string> OwnIdeasKeywords = new []{Keywords.AddIdeas, Keywords.RemoveIdeas}.ToFrozenSet();

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = ParseHelper.GetLeafValueNodesInChildren(_rootNode, OwnIdeasKeywords).ToList();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            UsedVariable = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_variable").ToList();
            
            var target = new ParserTargetKeywords();
            target.Bind(nameof(OwnIdeaLeaves), Keywords.AddIdeas, Keywords.RemoveIdeas);
            target.Bind(nameof(Capitals), Keywords.Capital);
            target.Bind(nameof(OwnOobs), "oob", "set_oob", "set_naval_oob", "set_air_oob", "load_oob");
            target.Bind(nameof(OwnCharacters), "recruit_character", "promote_character", "retire_character");
            target.Bind(nameof(UsedCountryTags), Keywords.Puppet, Keywords.EndPuppet, Keywords.AddToFaction,
                Keywords.GiveGuarantee, "remove_core_of", "remove_claim_by", "release_puppet", "release",
                "give_military_access");
            LeafContentParser.ParseValueToObject(target,  this, rootNode);
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
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
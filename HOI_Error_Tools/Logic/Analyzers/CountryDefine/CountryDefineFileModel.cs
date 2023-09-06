using System.Collections.Generic;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer
{
    private sealed class CountryDefineFileModel
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

        public CountryDefineFileModel(Node rootNode)
        {
            _rootNode = rootNode;

            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas();
            OwnIdeaLeaves = ParseHelper.GetLeafContentsInChildren(rootNode, "add_idea").ToList();
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            Capitals = ParseHelper.GetLeafContentsInChildren(rootNode, "capital").ToList();
            Puppets = ParseHelper.GetLeafContentsInChildren(rootNode, "puppet").ToList();
            CountriesTagOfAddToFaction = ParseHelper.GetLeafContentsInChildren(rootNode, "add_to_faction").ToList();
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
            GiveGuaranteeCountriesTag = ParseHelper.GetLeafContentsInChildren(rootNode, "give_guarantee").ToList();
            OwnCharacters = GetOwnCharacters();
            OwnOobs = GetOwnOobs();
        }

        private IReadOnlyList<LeafContent> GetOwnOobs()
        {
            var keywords = new HashSet<string>(4)
            {
                "oob",
                "set_oob",
                "set_naval_oob",
                "set_air_oob"
            };
            return ParseHelper.GetLeafContentsInChildren(_rootNode, keywords)
                .ToList();
        }

        private IReadOnlyList<LeafValueNode> GetOwnIdeas()
        {
            var set = new HashSet<string>(2)
            {
                "add_ideas",
                "remove_ideas",
            };
            return ParseHelper.GetLeafValueNodesInChildren(_rootNode, set)
                .ToList();
        }

        private IReadOnlyList<LeafContent> GetOwnCharacters()
        {
            var keywords = new HashSet<string>(3)
            {
                "recruit_character",
                "promote_character",
                "retire_character"
            };
            return ParseHelper.GetLeafContentsInChildren(_rootNode, keywords)
                .ToList();
        }
    }
}
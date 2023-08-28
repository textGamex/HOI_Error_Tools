using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
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
        public IReadOnlyList<LeavesNode> SetPoliticsList { get; }
        public IReadOnlyList<LeafContent> Capitals { get; }
        public IReadOnlyList<LeafContent> Puppets { get; }
        public IReadOnlyList<LeafContent> CountriesTagOfAddToFaction { get; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; }
        public IReadOnlyList<LeafContent> GiveGuaranteeCountriesTag { get; }
        public IReadOnlyList<LeafContent> OwnCharacters { get; }
        public IReadOnlyList<LeafContent> OwnOobs { get; }

        public CountryDefineFileModel(Node rootNode)
        {
            SetPopularitiesList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = GetOwnIdeas(rootNode);
            SetPoliticsList = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_politics").ToList();
            Capitals = ParseHelper.GetLeafContentsInAllChildren(rootNode, "capital").ToList();
            Puppets = ParseHelper.GetLeafContentsInAllChildren(rootNode, "puppet").ToList();
            CountriesTagOfAddToFaction = ParseHelper.GetLeafContentsInAllChildren(rootNode, "add_to_faction").ToList();
            SetAutonomies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafContentInRootNode(rootNode, "set_technology").ToList();
            GiveGuaranteeCountriesTag = ParseHelper.GetLeafContentsInAllChildren(rootNode, "give_guarantee").ToList();
            OwnCharacters = ParseHelper.GetLeafContentsInAllChildren(rootNode, "recruit_character").ToList();
            OwnOobs = GetOwnOobs(rootNode);
        }

        private static IReadOnlyList<LeafContent> GetOwnOobs(Node rootNode)
        {
            var keywords = new HashSet<string>(4)
            {
                "oob",
                "set_oob",
                "set_naval_oob",
                "set_air_oob"
            };
            return ParseHelper.GetLeafContentsInAllChildren(rootNode, keywords)
                .ToList();
        }

        private static IReadOnlyList<LeafValueNode> GetOwnIdeas(Node rootNode)
        {
            var set = new HashSet<string>(2)
            {
                "add_ideas",
                "remove_ideas",
            };
            return ParseHelper.GetLeafValueNodesInAllNode(rootNode, set)
                .ToList();
        }
    }
}
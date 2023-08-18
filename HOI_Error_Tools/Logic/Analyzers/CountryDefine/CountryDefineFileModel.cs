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
        public IReadOnlyList<LeavesNode> SetPoliticsList { get; }
        public IReadOnlyList<LeafContent> Capitals { get; }
        public IReadOnlyList<LeafContent> Puppets { get; }
        public IReadOnlyList<LeafContent> CountriesTagOfAddToFaction { get; }
        public IReadOnlyList<LeavesNode> SetAutonomies { get; }
        public IReadOnlyList<LeavesNode> SetTechnologies { get; }
        public IReadOnlyList<LeafContent> GiveGuaranteeCountriesTag { get; }
        public CountryDefineFileModel(Node rootNode)
        {
            SetPopularitiesList = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_popularities").ToList();
            OwnIdeaNodes = ParseHelper.GetLeafValueNodesInAllNode(rootNode, "add_ideas").ToList();
            SetPoliticsList = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_politics").ToList();
            Capitals = ParseHelper.GetLeafContentsInAllChildren(rootNode, "capital").ToList();
            Puppets = ParseHelper.GetLeafContentsInAllChildren(rootNode, "puppet").ToList();
            CountriesTagOfAddToFaction = ParseHelper.GetLeafContentsInAllChildren(rootNode, "add_to_faction").ToList();
            SetAutonomies = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_autonomy").ToList();
            SetTechnologies = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_technology").ToList();
            GiveGuaranteeCountriesTag = ParseHelper.GetLeafContentsInAllChildren(rootNode, "give_guarantee").ToList();
        }
    }
}
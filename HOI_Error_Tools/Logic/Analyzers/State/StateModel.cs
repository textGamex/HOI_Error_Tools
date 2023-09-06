using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer
{
    public sealed class StateModel
    {
        public IReadOnlyList<LeafContent> Ids { get; }
        public IReadOnlyList<LeafContent> Manpowers { get; }
        public IReadOnlyList<LeafContent> Names { get; }
        public IReadOnlyList<LeafContent> HasCoreTags { get; } = ImmutableList<LeafContent>.Empty;
        public IReadOnlyList<LeavesNodeWithCondition> BuildingNodes { get; } = ImmutableList<LeavesNodeWithCondition>.Empty;
        public IReadOnlyList<LeafContent> StateCategories { get; }
        public IReadOnlyList<LeafContent> Owners { get; } = ImmutableList<LeafContent>.Empty;
        public IReadOnlyList<LeavesNode> BuildingsByProvince { get; }
            = ImmutableList<LeavesNode>.Empty;
        public IReadOnlyList<LeavesNode> ResourceNodes { get; }
        public IReadOnlyList<LeafValueNode> ProvinceNodes { get; } 
        public IReadOnlyList<LeafValueNode> VictoryPointNodes { get; } = ImmutableList<LeafValueNode>.Empty;
        public bool IsEmptyFile { get; }

        public StateModel(Node rootNode)
        {
            if (rootNode.HasNot(ScriptKeyWords.State))
            {
                IsEmptyFile = true;
                return;
            }
            var stateNode = rootNode.Child(ScriptKeyWords.State).Value;

            Ids = ParseHelper.GetLeafContents(stateNode, ScriptKeyWords.Id).ToList();
            Manpowers = ParseHelper.GetLeafContents(stateNode, ScriptKeyWords.Manpower).ToList();
            Names = ParseHelper.GetLeafContents(stateNode, ScriptKeyWords.Name).ToList();
            StateCategories = ParseHelper.GetLeafContents(stateNode, ScriptKeyWords.StateCategory).ToList();
            ResourceNodes = ParseHelper.GetAllLeafContentInRootNode(stateNode, ScriptKeyWords.Resources).ToList();
            ProvinceNodes = ParseHelper.GetLeafValueNodesInAllNode(stateNode, ScriptKeyWords.Provinces).ToList();

            if (stateNode.HasNot(ScriptKeyWords.History))
            {
                return;
            }

            var historyNode = stateNode.Child(ScriptKeyWords.History).Value;
            BuildingNodes = ParseHelper.GetAllLeafContentWithConditionsInRootNode(historyNode, ScriptKeyWords.Buildings).ToList();
            var buildingsByProvince = new List<LeavesNode>();
            if (historyNode.Has(ScriptKeyWords.Buildings))
            {
                var buildingsNode = historyNode.Child(ScriptKeyWords.Buildings).Value;
                foreach (var provinceNode in buildingsNode.Nodes)
                {
                    var provinceBuildings = ParseHelper.GetAllLeafContentInCurrentNode(provinceNode);
                    buildingsByProvince.Add(
                        new LeavesNode(provinceNode.Key, provinceBuildings, new Position(provinceNode.Position)));
                }
            }

            BuildingsByProvince = buildingsByProvince;
            Owners = ParseHelper.GetLeafContents(historyNode, ScriptKeyWords.Owner).ToList();
            HasCoreTags = ParseHelper.GetLeafContentsInAllChildren(historyNode, "add_core_of").ToList();
            VictoryPointNodes = ParseHelper.GetLeafValueNodesInAllNode(historyNode, "victory_points").ToList();
        }
    }
}
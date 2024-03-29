﻿using System;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public sealed partial class StateFileAnalyzer
{
    public sealed class StateModel
    {
        public IReadOnlyList<LeafContent> Ids { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeafContent> Manpowers { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeafContent> Names { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeafContent> OwnCoreTags { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeavesNodeWithCondition> BuildingNodes { get; } = Array.Empty<LeavesNodeWithCondition>();
        public IReadOnlyList<LeafContent> StateCategories { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeafContentWithCondition> Owners { get; } = Array.Empty<LeafContentWithCondition>();
        public IReadOnlyList<LeavesNode> BuildingsByProvince { get; } = Array.Empty<LeavesNode>();
        public IReadOnlyList<LeavesNode> ResourceNodes { get; } = Array.Empty<LeavesNode>();
        public IReadOnlyList<LeafValueNode> ProvinceNodes { get; } = Array.Empty<LeafValueNode>();
        public IReadOnlyList<LeafValueNode> VictoryPointNodes { get; } = Array.Empty<LeafValueNode>();
        public IReadOnlyList<LeafContentWithCondition> ControllerTags { get; } = Array.Empty<LeafContentWithCondition>();
        public IReadOnlyList<LeafContent> ClaimCountryTags { get; } = Array.Empty<LeafContent>();
        public IReadOnlyList<LeafContent> LocalSupplies { get; } = Array.Empty<LeafContent>();
        public bool IsEmptyFile { get; }

        private static readonly ImmutableHashSet<string> LeafContentsKeywords = ImmutableHashSet.CreateRange(new []
        {
            ScriptKeyWords.Id,
            ScriptKeyWords.Manpower,
            ScriptKeyWords.Name,
            ScriptKeyWords.StateCategory,
            Keywords.LocalSupplies
        });

        public StateModel(Node rootNode)
        {
            if (rootNode.HasNot(ScriptKeyWords.State))
            {
                IsEmptyFile = true;
                return;
            }
            var stateNode = rootNode.Child(ScriptKeyWords.State).Value;
            
            var leavesMap = ParseHelper.GetLeafContentsByKeywordsInChildren(stateNode, LeafContentsKeywords);
            Ids = leavesMap[ScriptKeyWords.Id];
            Manpowers = leavesMap[ScriptKeyWords.Manpower];
            Names = leavesMap[ScriptKeyWords.Name];
            StateCategories = leavesMap[ScriptKeyWords.StateCategory];
            LocalSupplies = leavesMap[Keywords.LocalSupplies];
            ResourceNodes = ParseHelper.GetAllLeafContentInRootNode(stateNode, ScriptKeyWords.Resources).ToList();
            ProvinceNodes = ParseHelper.GetLeafValueNodesInChildren(stateNode, ScriptKeyWords.Provinces).ToList();
            
            if (stateNode.HasNot(ScriptKeyWords.History))
            {
                Log.Warn(CultureInfo.InvariantCulture,
                    "{FileName} 不含有 'history' 节点", stateNode.Position.FileName);
                return;
            }

            var historyNode = stateNode.Child(ScriptKeyWords.History).Value;
            BuildingNodes = ParseHelper.GetAllLeafContentWithConditionsInRootNode(historyNode, ScriptKeyWords.Buildings).ToList();
            ControllerTags = ParseHelper.GetLeafContentsWithConditionInChildren(historyNode, "controller").ToList();
            ClaimCountryTags = ParseHelper.GetLeafContentsInChildren(historyNode, "add_claim_by").ToList();
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
            Owners = ParseHelper.GetLeafContentsWithConditionInChildren(historyNode, ScriptKeyWords.Owner).ToList();
            OwnCoreTags = ParseHelper.GetLeafContentsInChildren(historyNode, "add_core_of").ToList();
            VictoryPointNodes = ParseHelper.GetLeafValueNodesInChildren(historyNode, "victory_points").ToList();
        }
    }
}
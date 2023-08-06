using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer
{
    private sealed class StateModel
    {
        public IReadOnlyList<(string Id, Position Position)> Ids { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Manpower, Position Position)> Manpowers { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Name, Position Position)> Names { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Tag, Position Position)> HasCoreTags { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<LeafContent> Buildings { get; } 
            = ImmutableList<LeafContent>.Empty;
        public IReadOnlyList<(string Type, Position Position)> StateCategories { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Owner, Position Position)> Owners { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string ProvinceId, IReadOnlyList<LeafContent> Buildings, Position Position)> BuildingsByProvince { get; } 
            = ImmutableList<(string, IReadOnlyList<LeafContent>, Position)>.Empty;
        public IReadOnlyList<(string ResourceName, string Amount, Position Position)> Resources { get; } 
            = ImmutableList<(string, string, Position)>.Empty;
        public IReadOnlyList<(string ProvinceId, Position Position)> Provinces { get; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(IReadOnlyList<string> VictoryPoints, Position Position)> VictoryPoints { get; } 
            = ImmutableList<(IReadOnlyList<string>, Position)>.Empty;
        public bool IsEmptyFile { get; }

        public StateModel(Node rootNode)
        {
            if (rootNode.HasNot(ScriptKeyWords.State))
            {
                IsEmptyFile = true;
                return;
            }
            var stateNode = rootNode.Child(ScriptKeyWords.State).Value;

            Ids = ParseHelper.GetLeavesValue(ScriptKeyWords.Id, stateNode).ToList();
            Manpowers = ParseHelper.GetLeavesValue(ScriptKeyWords.Manpower, stateNode).ToList();
            Names = ParseHelper.GetLeavesValue(ScriptKeyWords.Name, stateNode).ToList();
            StateCategories = ParseHelper.GetLeavesValue(ScriptKeyWords.StateCategory, stateNode).ToList();
            
            if (stateNode.Has(ScriptKeyWords.Resources))
            {
                var resourcesNode = stateNode.Child(ScriptKeyWords.Resources).Value;
                Resources = ParseHelper.GetLeavesKeyValuePairs(resourcesNode).Select(leaf => (leaf.Key, leaf.Value, leaf.Position)).ToList();
            }

            if (stateNode.Has(ScriptKeyWords.Provinces))
            {
                var provincesNode = stateNode.Child(ScriptKeyWords.Provinces).Value;
                Provinces = provincesNode.LeafValues
                    .Select(leaf => (leaf.ValueText, new Position(leaf.Position)))
                    .ToList();
            }

            if (stateNode.HasNot(ScriptKeyWords.History))
            {
                return;
            }

            var historyNode = stateNode.Child(ScriptKeyWords.History).Value;

            var buildingsByProvince = new List<(string, IReadOnlyList<LeafContent>, Position)>();
            if (historyNode.Has(ScriptKeyWords.Buildings))
            {
                var buildingsNode = historyNode.Child(ScriptKeyWords.Buildings).Value;
                foreach (var provinceNode in buildingsNode.Nodes)
                {
                    var provinceBuildings = ParseHelper.GetLeavesKeyValuePairs(provinceNode).ToList();
                    buildingsByProvince.Add((provinceNode.Key, provinceBuildings, new Position(provinceNode.Position)));
                }
                Buildings = ParseHelper.GetLeavesKeyValuePairs(buildingsNode).ToList();
            }

            BuildingsByProvince = buildingsByProvince;
            Owners = ParseHelper.GetLeavesValue(ScriptKeyWords.Owner, historyNode).ToList();
            HasCoreTags = ParseHelper.GetLeavesValue("add_core_of", historyNode).ToList();
            VictoryPoints = GetVictoryPoints(historyNode);
        }

        private static IReadOnlyList<(IReadOnlyList<string>, Position)> GetVictoryPoints(Node historyNode)
        {
            var victoryPoints = new List<(IReadOnlyList<string>, Position)>();
            var victoryPointsNodes = historyNode.Childs("victory_points");
            
            foreach (var victoryPointsNode in victoryPointsNodes)
            {
                var builder = victoryPointsNode.LeafValues.Select(leafValue => leafValue.Key).ToList();
                victoryPoints.Add((builder, new Position(victoryPointsNode.Position)));
            }
            return victoryPoints;
        }
    }
}
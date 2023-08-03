using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer
{
    private sealed class StateModel
    {
        //TODO: 用 ImmutableList 还是 ImmutableArray?
        public IReadOnlyList<(string Id, Position Position)> Ids { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Manpower, Position Position)> Manpowers { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Name, Position Position)> Names { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Tag, Position Position)> HasCoreTags { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string BuildingType, string Level, Position Position)> Buildings { get; private set; } 
            = ImmutableList<(string, string, Position)>.Empty;
        public IReadOnlyList<(string Type, Position Position)> StateCategories { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string Owner, Position Position)> Owners { get; private set; } = ImmutableList<(string, Position)>.Empty;
        public IReadOnlyList<(string ProvinceId, IReadOnlyList<(string BuildingName, string Level, Position Position)> Buildings, Position Position)> BuildingsByProvince { get; private set; } 
            = ImmutableList<(string, IReadOnlyList<(string, string, Position)>, Position)>.Empty;
        public IReadOnlyList<(string ResourceName, string Amount, Position Position)> Resources { get; private set; } 
            = ImmutableList<(string, string, Position)>.Empty;
        public IReadOnlyList<(string ProvinceId, Position Position)> Provinces { get; private set; } = ImmutableList<(string, Position)>.Empty;
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
                Resources = ParseHelper.GetLeavesKeyValuePairs(resourcesNode).ToList();
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

            var buildingsBuilder = ImmutableList.CreateBuilder<(string, string, Position)>();
            var buildingsByProvince = ImmutableList.CreateBuilder<(string, IReadOnlyList<(string, string, Position)>, Position)>();
            if (historyNode.Has(ScriptKeyWords.Buildings))
            {
                var buildingsNode = historyNode.Child(ScriptKeyWords.Buildings).Value;
                foreach (var leaf in buildingsNode.Leaves)
                {
                    buildingsBuilder.Add((leaf.Key, leaf.ValueText, new Position(leaf.Position)));
                }

                foreach (var provinceNode in buildingsNode.Nodes)
                {
                    var provinceBuildings = ParseHelper.GetLeavesKeyValuePairs(provinceNode).ToList();
                    buildingsByProvince.Add((provinceNode.Key, provinceBuildings, new Position(provinceNode.Position)));
                }
            }

            Buildings = buildingsBuilder.ToImmutable();
            BuildingsByProvince = buildingsByProvince.ToImmutable();
            Owners = ParseHelper.GetLeavesValue(ScriptKeyWords.Owner, historyNode).ToList();
            HasCoreTags = ParseHelper.GetLeavesValue("add_core_of", historyNode).ToList();
            VictoryPoints = GetVictoryPoints(historyNode);
        }

        private static IReadOnlyList<(IReadOnlyList<string>, Position)> GetVictoryPoints(Node historyNode)
        {
            var victoryPoints = ImmutableList.CreateBuilder<(IReadOnlyList<string>, Position)>();
            var victoryPointsNodes = historyNode.Childs("victory_points");
            
            foreach (var victoryPointsNode in victoryPointsNodes)
            {
                var builder = ImmutableList.CreateBuilder<string>();
                foreach (var leafValue in victoryPointsNode.LeafValues)
                {
                    builder.Add(leafValue.Key);
                }

                victoryPoints.Add((builder.ToImmutable(), new Position(victoryPointsNode.Position)));
            }
            return victoryPoints.ToImmutable();
        }
    }
}
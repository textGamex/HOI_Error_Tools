using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer
{
    private sealed class StateModel
    {
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
        public bool IsEmptyFile { get; private set; }

        public StateModel(Node rootNode)
        {
            if (rootNode.HasNot(ScriptKeyWords.State))
            {
                IsEmptyFile = true;
                return;
            }
            var stateNode = rootNode.Child(ScriptKeyWords.State).Value;

            Ids = GetLeavesValue(ScriptKeyWords.Id, stateNode);
            Manpowers = GetLeavesValue(ScriptKeyWords.Manpower, stateNode);
            Names = GetLeavesValue(ScriptKeyWords.Name, stateNode);
            StateCategories = GetLeavesValue(ScriptKeyWords.StateCategory, stateNode);
            
            if (stateNode.HasNot(ScriptKeyWords.History))
            {
                return;
            }

            if (stateNode.Has(ScriptKeyWords.Resources))
            {
                var resourcesNode = stateNode.Child(ScriptKeyWords.Resources).Value;
                Resources = GetLeavesKeyValuePairs(resourcesNode);
            }

            if (stateNode.Has(ScriptKeyWords.Provinces))
            {
                var provincesNode = stateNode.Child(ScriptKeyWords.Provinces).Value;
                Provinces = provincesNode.LeafValues
                    .Select(leaf => (leaf.ValueText, new Position(leaf.Position)))
                    .ToImmutableList();
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
                    var provinceBuildings = GetLeavesKeyValuePairs(provinceNode);
                    buildingsByProvince.Add((provinceNode.Key, provinceBuildings, new Position(provinceNode.Position)));
                }
            }
            Buildings = buildingsBuilder.ToImmutable();
            BuildingsByProvince = buildingsByProvince.ToImmutable();
            Owners = GetLeavesValue(ScriptKeyWords.Owner, historyNode);
            HasCoreTags = GetLeavesValue("add_core_of", historyNode);
        }
        
        private static ImmutableList<(string, Position)> GetLeavesValue(string key, Node node)
        {
            return node.Leafs(key)
                .Select(x => (x.ValueText, new Position(x.Position)))
                .ToImmutableList();
        }

        private static ImmutableList<(string, string, Position)> GetLeavesKeyValuePairs(Node node)
        {
            return node.Leaves
                .Select(x => (x.Key, x.ValueText, new Position(x.Position)))
                .ToImmutableList();
        }
    }
}
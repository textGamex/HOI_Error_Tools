using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer
{
    private sealed class StateModel
    {
        public IReadOnlyList<(string Tag, Position Position)> HasCoreTags { get; private set; }
        public IReadOnlyList<(string BuildingType, ushort Level, Position Position)> Buildings { get; private set; }
        public IReadOnlyList<(string StateCategory, string Type)> StateCategories { get; private set; }

        public StateModel(Node rootNode)
        {
            if (rootNode.HasNot(ScriptKeyWords.State))
            {
                SetEmpty();
                StateCategories = ImmutableList<(string, string)>.Empty;
                return;
            }

            var stateNode = rootNode.Child(ScriptKeyWords.State).Value;
            StateCategories = stateNode.Leafs(ScriptKeyWords.StateCategory)
                .Select(x => (x.Key, x.ValueText))
                .ToImmutableList();

            if (stateNode.HasNot(ScriptKeyWords.History))
            {
                SetEmpty();
                return;
            }

            var historyNode = stateNode.Child(ScriptKeyWords.History).Value;
            var buildingsBuilder = ImmutableList.CreateBuilder<(string, ushort, Position)>();
            if (historyNode.Has(ScriptKeyWords.Buildings))
            {
                foreach (var leaf in historyNode.Child(ScriptKeyWords.Buildings).Value.Leaves)
                {
                    buildingsBuilder.Add((leaf.Key, ushort.Parse(leaf.ValueText), new Position(leaf.Position)));
                }
            }
            HasCoreTags = historyNode.Leafs("add_core_of")
                .Select(x => (x.ValueText, new Position(x.Position)))
                .ToImmutableList();
            Buildings = buildingsBuilder.ToImmutable();

            void SetEmpty()
            {
                HasCoreTags = ImmutableList<(string, Position)>.Empty;
                Buildings = ImmutableList<(string, ushort, Position)>.Empty;
            }
        }
    }
}
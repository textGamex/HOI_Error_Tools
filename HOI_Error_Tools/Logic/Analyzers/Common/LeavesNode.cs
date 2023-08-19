using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeavesNode
{
    public string Key { get; }
    public IEnumerable<LeafContent> Leaves { get; }
    public Position Position { get; }

    public LeavesNode(string key, IEnumerable<LeafContent> leaves, Position position)
    {
        Key = key;
        Leaves = leaves;
        Position = position;
    }
}
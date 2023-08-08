using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeavesNode
{
    public IEnumerable<LeafContent> Leaves { get; }
    public Position Position { get; }

    public LeavesNode(IEnumerable<LeafContent> leaves, Position position)
    {
        Leaves = leaves;
        Position = position;
    }
}
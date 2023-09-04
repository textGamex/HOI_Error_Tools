using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeavesNodeWithCondition : LeavesNode
{
    public Condition Condition { get; }
    public LeavesNodeWithCondition(string key, IEnumerable<LeafContent> leaves, Position position, Condition condition) 
        : base(key, leaves, position)
    {
        Condition = condition;
    }
}
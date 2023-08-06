using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafValueNode
{
    public IEnumerable<LeafValueContent> LeafValueContents { get; }
    public Position Position { get; }

    public LeafValueNode(IEnumerable<LeafValueContent> leafValueContents, Position position)
    {
        LeafValueContents = leafValueContents;
        Position = position;
    }
}
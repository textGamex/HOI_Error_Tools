using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafValueNode
{
    public string Key { get; }
    public IEnumerable<LeafValueContent> LeafValueContents { get; }
    public Position Position { get; }

    public LeafValueNode(string key, IEnumerable<LeafValueContent> leafValueContents, Position position)
    {
        Key = key;
        LeafValueContents = leafValueContents;
        Position = position;
    }
}
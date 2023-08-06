using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafValueContent
{
    public string Value { get; }
    public Position Position { get; }

    private LeafValueContent(string value, Position position)
    {
        Value = value;
        Position = position;
    }

    public static LeafValueContent FromCWToolsLeafValue(LeafValue leafValue)
    {
        return new LeafValueContent(leafValue.Key, new Position(leafValue.Position));
    }

    public override string ToString()
    {
        return $"{nameof(Value)}={Value}, {nameof(Position)}={Position}";
    }
}
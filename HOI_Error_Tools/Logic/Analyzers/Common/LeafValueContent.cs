﻿using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafValueContent
{
    public Value Value { get; }
    public string ValueText => Value.Text;
    public Position Position { get; }

    public LeafValueContent(Value value, Position position)
    {
        Value = value;
        Position = position;
    }

    public static LeafValueContent FromCWToolsLeafValue(LeafValue leafValue)
    {
        return new LeafValueContent(Value.FromCWToolsValue(leafValue.Value), new Position(leafValue.Position));
    }

    public override string ToString()
    {
        return $"{nameof(Value)}={Value}, {nameof(Position)}={Position}";
    }
}
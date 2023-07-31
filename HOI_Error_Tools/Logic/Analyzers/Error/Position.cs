using System;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class Position
{
    public long Line { get; }
    public static Position Empty => _empty;
    private static readonly Position _empty = new(-1);

    public Position(CWTools.Utilities.Position.range position)
    {
        Line = position.StartLine;
    }

    public Position(CWTools.CSharp.ParserError error)
    {
        Line = error.Line;
    }

    private Position(long line)
    {
        Line = line;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line);
    }
}
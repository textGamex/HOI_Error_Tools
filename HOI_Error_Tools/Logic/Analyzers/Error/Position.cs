using System;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public class Position : IEquatable<Position>
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

    public Position(long line)
    {
        Line = line;
    }

    public override int GetHashCode()
    {
        return Line.GetHashCode();
    }

    public bool Equals(Position? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Line == other.Line;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Position)obj);
    }

    public static bool operator ==(Position? left, Position? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Position? left, Position? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"{nameof(Line)}={Line}";
    }
}
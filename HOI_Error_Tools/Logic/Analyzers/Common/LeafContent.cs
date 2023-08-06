using System;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafContent : IEquatable<LeafContent>
{
    public string Key { get; }
    public string Value { get; }
    public Position Position { get; }

    public static LeafContent FromCWToolsLeaf(CWTools.Process.Leaf leaf)
    {
        return new LeafContent(leaf.Key, leaf.ValueText, new Position(leaf.Position));
    }

    public LeafContent(string key, string value, Position position)
    {
        Key = key;
        Value = value;
        Position = position;
    }

    public bool Equals(LeafContent? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Value == other.Value && Position.Equals(other.Position);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((LeafContent)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Value, Position);
    }

    public static bool operator ==(LeafContent? left, LeafContent? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LeafContent? left, LeafContent? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"{nameof(Key)}={Key}, {nameof(Value)}={Value}, {nameof(Position)}={Position}";
    }
}
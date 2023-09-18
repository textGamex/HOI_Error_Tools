using HOI_Error_Tools.Logic.Analyzers.Error;
using System;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeafContent : IEquatable<LeafContent>
{
    public string Key { get; }
    public Value Value { get; }
    public string ValueText => Value.Text;
    public Position Position { get; }

    public static LeafContent FromCWToolsLeaf(CWTools.Process.Leaf leaf)
    {
        return new LeafContent(leaf.Key, Value.FromCWToolsValue(leaf.Value), new Position(leaf.Position));
    }

    public LeafContent(string key, string valueText, Position position)
    {
        Key = key;
        Value = Value.FromString(valueText);
        Position = position;
    }

    public LeafContent(string key, Value value, Position position)
    {
        Key = key;
        Value = value;
        Position = position;
    }

    public bool Equals(LeafContent? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key && Value == other.Value && Position == other.Position;
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
        return $"[{nameof(Key)}={Key}, {nameof(ValueText)}={ValueText}, {nameof(Position)}={Position}]";
    }

    private sealed class LeafContentEqualityComparer : IEqualityComparer<LeafContent>
    {
        public bool Equals(LeafContent? x, LeafContent? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Equals(y);
        }

        public int GetHashCode(LeafContent obj)
        {
            return obj.GetHashCode();
        }
    }

    public static IEqualityComparer<LeafContent> LeafContentComparer { get; } = new LeafContentEqualityComparer();
}
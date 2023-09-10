using System;
using System.Collections.Generic;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class LeavesNode : IEquatable<LeavesNode>
{
    public string Key { get; }
    public IEnumerable<LeafContent> Leaves { get; }
    public Position Position { get; }

    public LeavesNode(string key, IEnumerable<LeafContent> leaves, Position position)
    {
        Key = key;
        Leaves = leaves;
        Position = position;
    }

    public bool Equals(LeavesNode? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Key == other.Key &&
             Leaves.SequenceEqual(other.Leaves, LeafContent.LeafContentComparer) &&
             Position.Equals(other.Position);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (this.GetType() != obj.GetType()) return  false;
        
        return Equals((LeavesNode)obj);
    }

    public override int GetHashCode()
    {
        int hash = Leaves.Aggregate(31, (current, leaf) => current * 17 + leaf.GetHashCode());
        return HashCode.Combine(Key, hash, Position);
    }
}
using System;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class Condition : IEquatable<Condition>
{
    public static Condition Empty { get; } = new(default);
    public DateOnly Date { get; }

    public Condition(DateOnly date)
    {
        Date = date;
    }

    public override string ToString()
    {
        return $"{nameof(Date)}={Date}";
    }

    public bool Equals(Condition? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Date.Equals(other.Date);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Condition)obj);
    }

    public override int GetHashCode()
    {
        return Date.GetHashCode();
    }

    public static bool operator ==(Condition? left, Condition? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Condition? left, Condition? right)
    {
        return !Equals(left, right);
    }
}
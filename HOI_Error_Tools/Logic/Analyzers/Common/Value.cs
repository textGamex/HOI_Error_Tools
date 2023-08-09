using System;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class Value : IEquatable<Value>
{
    public string Text { get; }
    public bool IsInt => _type == ValueType.Int;
    public bool IsBoolean => _type == ValueType.Boolean;
    public bool IsString => _type == ValueType.String;
    public bool IsFloat => _type == ValueType.Float;
    public bool IsNumber => _type is ValueType.Int or ValueType.Float;
    public bool IsNegativeNumber => IsNumber && Text.StartsWith('-');

    private readonly ValueType _type;

    private Value(string text, ValueType type)
    {
        Text = text;
        _type = type;
    }

    public static Value FromCWToolsValue(CWTools.Parser.Types.Value value)
    {
        return new Value(value.ToRawString(), GetValueTypeFromValueTag(value.Tag));
    }

    public static Value FromString(string text)
    {
        return new Value(text, GetValueTypeFromString(text));
    }

    private static ValueType GetValueTypeFromString(string text)
    {
        if (int.TryParse(text, out _))
        {
            return ValueType.Int;
        }
        else if (float.TryParse(text, out _))
        {
            return ValueType.Float;
        }
        else if (text is "yes" or "no")
        {
            return ValueType.Boolean;
        }
        else
        {
            return ValueType.String;
        }
    }

    private static ValueType GetValueTypeFromValueTag(int valueTag) => valueTag switch
    {
       0 => ValueType.String,
       1 => ValueType.String,
       2 => ValueType.Float,
       3 => ValueType.Int,
       4 => ValueType.Boolean,
       _ => throw new ArgumentOutOfRangeException(nameof(valueTag), valueTag, null)
    };
    

    private enum ValueType : byte
    {
        Int,
        Boolean,
        String,
        Float
    }

    public bool Equals(Value? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _type == other._type && Text == other.Text;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Value)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)_type, Text);
    }

    public static bool operator ==(Value? left, Value? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Value? left, Value? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"{nameof(Text)}={Text}, Type={_type}";
    }
}
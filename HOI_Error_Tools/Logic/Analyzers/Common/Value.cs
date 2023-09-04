using System;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class Value : IEquatable<Value>
{
    public string Text { get; }
    public bool IsInt => Type == Types.Integer;
    public bool IsBoolean => Type == Types.Boolean;
    public bool IsString => Type == Types.String;
    public bool IsFloat => Type == Types.Float;
    public bool IsDate => Type == Types.Date;
    public bool IsNumber => Type is Types.Integer or Types.Float;
    public bool IsNegativeNumber => IsNumber && Text.StartsWith('-');
    public Types Type { get; }

    private Value(string text, Types type)
    {
        Text = text;
        Type = type;
    }

    public static Value FromCWToolsValue(CWTools.Parser.Types.Value value)
    {
        var valueType = GetValueTypeFromValueTag(value.Tag);
        var text = value.ToRawString();
        if (valueType == Types.String)
        {
            valueType = IsDateString(text) ? Types.Date : valueType;
        }
        return new Value(text, valueType);
    }

    public static Value FromString(string text)
    {
        return new Value(text, GetValueTypeFromString(text));
    }

    private static Types GetValueTypeFromString(string text)
    {
        if (int.TryParse(text, out _))
        {
            return Types.Integer;
        }
        else if (float.TryParse(text, out _))
        {
            return Types.Float;
        }
        else if (text is ScriptKeyWords.Yes or ScriptKeyWords.No)
        {
            return Types.Boolean;
        }
        else if (IsDateString(text))
        {
            return Types.Date;
        }
        else
        {
            return Types.String;
        }
    }

    private static Types GetValueTypeFromValueTag(int valueTag) => valueTag switch
    {
       0 => Types.String,
       1 => Types.String,
       2 => Types.Float,
       3 => Types.Integer,
       4 => Types.Boolean,
       _ => throw new ArgumentOutOfRangeException(nameof(valueTag), valueTag, null)
    };
    

    public enum Types : byte
    {
        Integer,
        Boolean,
        String,
        Float,
        Date
    }

    public static bool IsDateString(string text)
    {
        return DateOnly.TryParse(text, out _);
    }

    public static bool TryParseDate(string text, out DateOnly date)
    {
        return DateOnly.TryParse(text, out date);
    }

    public bool Equals(Value? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type && Text == other.Text;
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
        return HashCode.Combine((int)Type, Text);
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
        return $"[{nameof(Text)}={Text}, Type={Type}]";
    }
}
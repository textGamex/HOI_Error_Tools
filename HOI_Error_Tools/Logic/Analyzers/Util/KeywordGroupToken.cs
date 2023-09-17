using System;
using System.Collections.Generic;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public sealed partial class ParserTargetKeywords
{
    public class KeywordGroupToken : IEquatable<KeywordGroupToken>
    {
        private readonly int _token;
        public KeywordGroupToken(IEnumerable<string> keywords)
        {
            _token = keywords.Select(x => x.GetHashCode()).Aggregate((x, y) => x ^ y);
        }

        public KeywordGroupToken(string keyword)
        {
            _token = keyword.GetHashCode();
        }

        public bool Equals(KeywordGroupToken? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _token == other._token;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (GetType() != obj.GetType()) return false;
            return Equals((KeywordGroupToken)obj);
        }

        public override int GetHashCode()
        {
            return _token;
        }

        public static bool operator ==(KeywordGroupToken? left, KeywordGroupToken? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(KeywordGroupToken? left, KeywordGroupToken? right)
        {
            return !Equals(left, right);
        }
    }
}

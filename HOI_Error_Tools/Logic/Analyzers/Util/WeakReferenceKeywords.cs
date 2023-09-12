using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public class WeakReferenceKeywords
{
    public IReadOnlySet<string> Keywords
    {
        get
        {
            if (!_weakReference.TryGetTarget(out var keys))
            {
                keys = ImmutableHashSet.CreateRange(_keywords());
                _weakReference.SetTarget(keys);
            }
            return keys;
        }
    }
    private readonly WeakReference<ImmutableHashSet<string>?> _weakReference = new(null);
    private readonly Func<IEnumerable<string>> _keywords;
    
    public WeakReferenceKeywords(Func<IEnumerable<string>> keywords)
    {
        _keywords = keywords;
    }
}
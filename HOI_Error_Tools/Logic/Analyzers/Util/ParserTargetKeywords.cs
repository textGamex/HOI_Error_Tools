using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.Logic.Analyzers.Util;

public sealed partial class ParserTargetKeywords
{
    public IReadOnlyDictionary<KeywordGroupToken, List<string>> TargetKeywords => _map;
    public IEnumerable<(string PropertyName, KeywordGroupToken Value)> ObjectPropertiesSetter => _propertiesValue;
    public IReadOnlySet<string> Keywords => _map.SelectMany(item => item.Value).ToHashSet();
    private readonly Dictionary<KeywordGroupToken, List<string>> _map = new(8);
    private readonly List<(string PropertyName, KeywordGroupToken Value)> _propertiesValue = new(8);
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public void Bind(string propertyName, params string[] keywordsValue)
    {
        var token = new KeywordGroupToken(keywordsValue);
        var keywords = keywordsValue.Distinct().ToList();
#if DEBUG
        if (keywords.Count != keywordsValue.Length)
        {
            var repeated = keywordsValue.ToList();
            foreach (var item in keywords)
            {
                repeated.Remove(item);
            }
            foreach (var item in repeated)
            {
                Log.Warn(CultureInfo.InvariantCulture, "重复的关键字: {Key}", item);
            }
        }
#endif
        _map.Add(token, keywords);
        _propertiesValue.Add((propertyName, token));
    }

    public void Bind(string propertyName, string keyword)
    {
        var token = new KeywordGroupToken(keyword);
        _map.Add(token, new List<string>(1) { keyword });
        _propertiesValue.Add((propertyName, token));
    }
}
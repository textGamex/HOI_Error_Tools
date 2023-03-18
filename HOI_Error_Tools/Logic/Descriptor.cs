using System.IO;
using System.Collections.Generic;
using System.Linq;
using HOI_Error_Tools.Logic.HOIParser;
using HOI_Error_Tools.Logic.CustomException;
using HOI_Error_Tools.Logic.Analyzers;

namespace HOI_Error_Tools.Logic;

/// <summary>
/// MOD描述文件类
/// </summary>
public class Descriptor
{
    public string Name { get; } = string.Empty;

    public string SupportedVersion { get; } = string.Empty;
    public string Version { get; } = string.Empty;

    public IEnumerable<string> Tags { get; }

    public string PictureName { get; } = string.Empty;

    public IEnumerable<string> ReplacePaths => _replacePaths;
    private readonly List<string> _replacePaths;

    /// <summary>
    /// 按文件绝对路径构建
    /// </summary>
    /// <param name="modRootPath">游戏根目录绝对路径</param>
    /// <exception cref="ParseException">当文件解析失败时</exception>
    /// <exception cref="FileNotFoundException">当文件不存在时</exception>
    public Descriptor(string modRootPath)
    {
        var path = Path.Combine(modRootPath, "descriptor.mod");
        var parser = new Parser(path);
        if (parser.IsFailure)
        {
            throw new ParseException($"解析失败 => {path}");
        }

        _replacePaths = new List<string>();
        var root = parser.GetResult();
        var result = root.Leaves;

        foreach (var item in result)
        {
            switch (item.Key)
            {
                case ScriptKeyWords.Name:
                    Name = item.ValueText;
                    break;
                case ScriptKeyWords.SupportedVersion:
                    SupportedVersion = item.ValueText;
                    break;
                case ScriptKeyWords.Picture:
                    PictureName = item.ValueText;
                    break;
                case ScriptKeyWords.Version:
                    Version = item.ValueText;
                    break;
                case ScriptKeyWords.ReplacePath:
                    _replacePaths.Add(item.ValueText);
                    break;
            }
        }
        _replacePaths.TrimExcess();

        if (root.Has(ScriptKeyWords.Tags))
        {
            var tags = root.Child(ScriptKeyWords.Tags).Value;
            var list = tags.LeafValues.Select(x => x.Key).ToList();
            list.TrimExcess();
            Tags = list;
        }
        else
        {
            Tags = Enumerable.Empty<string>();
        }
    }
}
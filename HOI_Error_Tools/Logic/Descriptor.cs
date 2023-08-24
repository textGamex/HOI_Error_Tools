using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.HOIParser;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using NLog;

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
    public BitmapImage? Picture { get; }
    public string RemoteFileId { get; } = string.Empty;

    /// <summary>
    /// 保存着替换的文件夹相对路径的只读集合
    /// </summary>
    /// <remarks>
    /// 线程安全
    /// </remarks>
    public IReadOnlySet<string> ReplacePaths => _replacePaths;
    private readonly ImmutableHashSet<string> _replacePaths;
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    /// <summary>
    /// 按文件绝对路径构建
    /// </summary>
    /// <param name="modRootPath">游戏根目录绝对路径</param>
    /// <exception cref="FileNotFoundException">当文件不存在时</exception>
    public Descriptor(string modRootPath)
    {
        var path = Path.Combine(modRootPath, "descriptor.mod");
        var parser = new CWToolsParser(path);
        if (parser.IsFailure)
        {
            Tags = Enumerable.Empty<string>();
            _replacePaths = ImmutableHashSet<string>.Empty;
            Log.Warn("Mod descriptor.mod file read is failure");
            return;
        }

        var replacePathsBuilder = ImmutableHashSet.CreateBuilder<string>();

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
                    var parts = item.ValueText.Split('/');
                    replacePathsBuilder.Add(Path.Combine(parts));
                    break;
                case "remote_file_id":
                    RemoteFileId = item.ValueText;
                    break;
            }
        }
        _replacePaths = replacePathsBuilder.ToImmutable();

        if (root.Has(ScriptKeyWords.Tags))
        {
            var tags = root.Child(ScriptKeyWords.Tags).Value;
            Tags = tags.LeafValues.Select(x => x.Key).ToList();
        }
        else
        {
            Tags = Enumerable.Empty<string>();
        }
        Picture = TryGetBitmapImage(modRootPath);
    }

    private static BitmapImage? TryGetBitmapImage(string modRootPath)
    {
        var imagePath = Path.Combine(modRootPath, "thumbnail.png");
        if (!File.Exists(imagePath))
        {
            return null;
        }
        var newImage = new BitmapImage();
        using var ms = new MemoryStream(File.ReadAllBytes(imagePath));
        newImage.BeginInit();
        newImage.CacheOption = BitmapCacheOption.OnLoad;
        newImage.StreamSource = ms;
        newImage.EndInit();
        newImage.Freeze();
        return newImage;
    }
}
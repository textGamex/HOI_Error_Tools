using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers;

namespace HOI_Error_Tools.Logic;

/// <summary>
/// 管理游戏资源路径
/// </summary>
public sealed class GameResourcesPath
{
    public string GameRootPath { get; }
    public string ModRootPath { get; }
    public string GameLocPath { get; }
    public string ModLocPath { get; }
    public string ProvincesDefinitionFilePath { get; }
    public IImmutableList<string> BuildingsFilePathList => _buildingsFilePathList;
    public IImmutableList<string> StatesPathList => _statesPathList;

    private readonly ImmutableList<string> _statesPathList;
    private readonly ImmutableList<string> _buildingsFilePathList;
    private readonly Descriptor _descriptor;

    public GameResourcesPath(string gameRootPath, string modRootPath)
    {
        if (!Directory.Exists(gameRootPath))
        {
            throw new DirectoryNotFoundException($"文件夹不存在 {gameRootPath}");
        }
        if (!Directory.Exists(modRootPath))
        {
            throw new DirectoryNotFoundException($"文件夹不存在 {modRootPath}");
        }

        GameRootPath = gameRootPath;
        ModRootPath = modRootPath;
        GameLocPath = GetLocPath(GameRootPath);
        ModLocPath = GetLocPath(ModRootPath);
        ProvincesDefinitionFilePath = GetFilePathPriorModByRelativePath(Path.Combine(Key.Map, "definition.csv"));

        _descriptor = new Descriptor(modRootPath);
        _buildingsFilePathList = ImmutableList.CreateRange(
            GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, Key.Buildings)));
        _statesPathList = ImmutableList.CreateRange(
            GetAllFilePriorModByRelativePathForFolder(Path.Combine(ScriptKeyWords.History, Key.States)));
    }

    private static string GetLocPath(string rootPath)
    {
        return Path.Combine(rootPath, "localisation");
    }

    /// <summary>
    /// 根据相对路径获得游戏或者Mod文件的绝对路径, 优先Mod
    /// </summary>
    /// <remarks>
    /// 注意: 此方法会忽略mod描述文件中的 replace_path 指令
    /// </remarks>
    /// <param name="fileRelativePath">根目录下的相对路径</param>
    /// <exception cref="FileNotFoundException">游戏和mod中均不存在</exception>
    /// <returns></returns>
    private string GetFilePathPriorModByRelativePath(string fileRelativePath)
    {
        var modFilePath = Path.Combine(ModRootPath, fileRelativePath);
        if (File.Exists(modFilePath))
        {
            return modFilePath;
        }

        var gameFilePath = Path.Combine(GameRootPath, fileRelativePath);
        if (File.Exists(gameFilePath))
        {
            return gameFilePath;
        }
        
        throw new FileNotFoundException($"在Mod和游戏中均找不到目标文件 '{fileRelativePath}'");
    }

    /// <summary>
    /// 获得所有应该加载的文件绝对路径, Mod优先, 遵循 replace_path 指令
    /// </summary>
    /// <param name="folderRelativePath"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    private IEnumerable<string> GetAllFilePriorModByRelativePathForFolder(string folderRelativePath)
    {
        var modFolder = Path.Combine(ModRootPath, folderRelativePath);
        var gameFolder = Path.Combine(GameRootPath, folderRelativePath);

        if (!Directory.Exists(gameFolder))
        {
            throw new DirectoryNotFoundException($"找不到文件夹 {gameFolder}");
        }

        if (!Directory.Exists(modFolder))
        {
            return GetAllFilePathForFolder(gameFolder);
        }

        if (_descriptor.ReplacePaths.Contains(folderRelativePath))
        {
            return GetAllFilePathForFolder(modFolder);
        }

        var gameFilesPath = GetAllFilePathForFolder(gameFolder);
        var modFilesPath = GetAllFilePathForFolder(modFolder);
        return RemoveFileOfEqualName(gameFilesPath, modFilesPath);
    }

    /// <summary>
    /// 获得一个文件夹下的所有文件
    /// </summary>
    /// <param name="folderPath"></param>
    /// <returns></returns>
    private static IEnumerable<string> GetAllFilePathForFolder(string folderPath)
    {
        Debug.Assert(Directory.Exists(folderPath), $"文件夹不存在 {folderPath}");

        var dir = new DirectoryInfo(folderPath);
        var files = dir.GetFiles();
        return files.Select(f => f.FullName).ToList();
    }

    /// <summary>
    /// 移除重名文件, 优先移除游戏本体文件
    /// </summary>
    /// <param name="gameFilePathList"></param>
    /// <param name="modFilePathList"></param>
    /// <returns></returns>
    /// <exception cref="FileFormatException"></exception>
    private static IEnumerable<string> RemoveFileOfEqualName(IEnumerable<string> gameFilePathList, IEnumerable<string> modFilePathList)
    {
        var set = new HashSet<string>();

        // 优先读取Mod文件
        // 使用 ChatGPT 生成
        foreach (var filePath in modFilePathList.Concat(gameFilePathList))
        {
            var fileName = Path.GetFileName(filePath) ?? throw new FileFormatException($"无法得到文件名 {filePath}");
            if (!set.Contains(fileName))
            {
                set.Add(fileName);
                yield return filePath;
            }
        }
    }

    private static class Key
    {
        public const string States = "states";
        public const string Map = "map";
        public const string Common = "common";
        public const string Buildings = "buildings";
    }
}
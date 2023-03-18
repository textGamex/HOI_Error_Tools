using System.Collections.Generic;
using System.IO;
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
    public IReadOnlyList<string> StatesPathList => _statesPathList.AsReadOnly();
    private readonly List<string> _statesPathList;
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
        ProvincesDefinitionFilePath = GetFilePathPriorModByRelativePath(Path.Combine("map", "definition.csv"));

        _descriptor = new Descriptor(modRootPath);
        _statesPathList = GetStatesFilePathList().ToList();
    }

    private static string GetLocPath(string rootPath)
    {
        return Path.Combine(rootPath, "localisation");
    }

    /// <summary>
    /// 根据相对路径获得游戏或者Mod文件的绝对路径, 优先Mod
    /// </summary>
    /// <param name="relativePath">根目录下的相对路径</param>
    /// <exception cref="FileNotFoundException">游戏和mod中均不存在</exception>
    /// <returns></returns>
    private string GetFilePathPriorModByRelativePath(string relativePath)
    {
        var modFilePath = Path.Combine(ModRootPath, relativePath);
        if (File.Exists(modFilePath))
        {
            return modFilePath;
        }

        var gameFilePath = Path.Combine(GameRootPath, relativePath);
        if (File.Exists(gameFilePath))
        {
            return gameFilePath;
        }
        
        throw new FileNotFoundException($"在Mod和游戏中均找不到目标文件 '{relativePath}'");
    }

    private IEnumerable<string> GetStatesFilePathList()
    {
        var gameFolder = Path.Combine(GameRootPath, ScriptKeyWords.History, Key.States);
        var modFolder = Path.Combine(ModRootPath, ScriptKeyWords.History, Key.States);

        if (_descriptor.ReplacePaths.Contains("history/states"))
        {
            return GetAllFilePathForFolder(modFolder);
        }

        var modFilePathList = GetAllFilePathForFolder(modFolder);
        var gameFilePathList = GetAllFilePathForFolder(gameFolder);
        return RemoveFileOfEqualName(gameFilePathList, modFilePathList);
    }

    private static IEnumerable<string> GetAllFilePathForFolder(string folderPath)
    {
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
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers;
using NLog;

namespace HOI_Error_Tools.Logic;

/// <summary>
/// 管理游戏资源路径
/// </summary>
public sealed class GameResourcesPath
{
    public string GameRootFolderPath { get; }
    public string ModRootFolderPath { get; }
    public string GameLocPath { get; }
    public string ModLocPath { get; }
    public string ProvincesDefinitionFilePath { get; }
    public IReadOnlyList<string> StateCategoriesFilePath { get; }
    public IReadOnlyList<string> CountriesDefineFilePath { get; }
    public IReadOnlyList<string> IdeaFilesPath { get; }
    public IReadOnlyList<string> IdeaTagsFilePath { get; }
    //public IReadOnlyList<string> EquipmentFilesPath { get; }
    public IReadOnlyList<string> TechnologyFilesPath { get; }
    public IReadOnlyList<string> CountriesTagFilePath { get; }
    public IReadOnlyList<string> IdeologiesFilePath { get; }
    public IReadOnlyList<string> AutonomousStateFilesPath { get; }
    public IReadOnlyList<string> CharactersFilesPath { get; }
    public IReadOnlyList<string> OobFilesPath { get; }
    public IReadOnlyList<string> BuildingsFilePathList => _buildingsFilePathList;
    public IReadOnlyList<string> ResourcesTypeFilePathList => _resourcesTypeFilePathList;
    public IReadOnlyList<string> StatesFilePathList => _statesFilePathList;

    public int FileSum { get; }

    private readonly ImmutableList<string> _statesFilePathList;
    private readonly ImmutableList<string> _buildingsFilePathList;
    private readonly ImmutableList<string> _resourcesTypeFilePathList;
    private readonly Descriptor _descriptor;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GameResourcesPath(string gameRootFolderPath, string modRootFolderPath) 
        : this(gameRootFolderPath, modRootFolderPath, new Descriptor(Path.Combine(modRootFolderPath, "descriptor.mod")))
    {
    }

    public GameResourcesPath(string gameRootFolderPath, string modRootFolderPath, Descriptor descriptor)
    {
        if (!Directory.Exists(gameRootFolderPath))
        {
            throw new DirectoryNotFoundException($"文件夹不存在: {gameRootFolderPath}");
        }
        if (!Directory.Exists(modRootFolderPath))
        {
            throw new DirectoryNotFoundException($"文件夹不存在: {modRootFolderPath}");
        }

        GameRootFolderPath = gameRootFolderPath;
        ModRootFolderPath = modRootFolderPath;
        _descriptor = descriptor;
        GameLocPath = GetLocPath(GameRootFolderPath);
        ModLocPath = GetLocPath(ModRootFolderPath);
        ProvincesDefinitionFilePath = GetFilePathPriorModByRelativePath(Path.Combine(Key.Map, "definition.csv"));

        StateCategoriesFilePath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, ScriptKeyWords.StateCategory)).ToList();
        CountriesDefineFilePath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(ScriptKeyWords.History, "countries")).ToList();
        IdeaFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, ScriptKeyWords.Ideas)).ToList();
        IdeaTagsFilePath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "idea_tags")).ToList();
        //EquipmentFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "units", "equipment"))
        //    .Where(path => Path.GetExtension(path) == ".txt").ToList();
        TechnologyFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "technologies")).ToList();
        AutonomousStateFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "autonomous_states")).ToList();
        CharactersFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, ScriptKeyWords.Characters)).ToList();
        OobFilesPath = GetAllFilePriorModByRelativePathForFolder(Path.Combine(ScriptKeyWords.History, "units")).ToList();

        CountriesTagFilePath = ImmutableList.CreateRange(GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "country_tags")));
        IdeologiesFilePath = ImmutableList.CreateRange(GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, ScriptKeyWords.Ideologies)));

        _buildingsFilePathList = ImmutableList.CreateRange(
            GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, ScriptKeyWords.Buildings)));
        _statesFilePathList = ImmutableList.CreateRange(
            GetAllFilePriorModByRelativePathForFolder(Path.Combine(ScriptKeyWords.History, Key.States)));
        _resourcesTypeFilePathList = ImmutableList.CreateRange(
            GetAllFilePriorModByRelativePathForFolder(Path.Combine(Key.Common, "resources")));

        FileSum = GetFileSum();
    }

    private int GetFileSum()
    {
        return StateCategoriesFilePath.Count + CountriesDefineFilePath.Count +
            IdeaFilesPath.Count + IdeaTagsFilePath.Count + TechnologyFilesPath.Count +
            CountriesTagFilePath.Count + IdeologiesFilePath.Count +
            BuildingsFilePathList.Count + ResourcesTypeFilePathList.Count +
            StatesFilePathList.Count + AutonomousStateFilesPath.Count +
            CharactersFilesPath.Count + OobFilesPath.Count + 1; // 这个 1 是 ProvincesDefinitionFilePath 文件
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
    /// <returns>文件路径</returns>
    private string GetFilePathPriorModByRelativePath(string fileRelativePath)
    {
        var modFilePath = Path.Combine(ModRootFolderPath, fileRelativePath);
        if (File.Exists(modFilePath))
        {
            return modFilePath;
        }

        var gameFilePath = Path.Combine(GameRootFolderPath, fileRelativePath);
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
        Log.Debug(CultureInfo.InvariantCulture, "正在加载文件夹: {Path}", folderRelativePath);
        var modFolder = Path.Combine(ModRootFolderPath, folderRelativePath);
        var gameFolder = Path.Combine(GameRootFolderPath, folderRelativePath);

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
            Log.Debug(CultureInfo.InvariantCulture,
                "MOD文件夹已完全替换游戏文件夹: \n\t {GamePath} => {ModPath}", gameFolder.ToFilePath(), modFolder.ToFilePath());
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
        return Directory.GetFiles(folderPath);
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
            var fileName = Path.GetFileName(filePath) ?? throw new FileFormatException($"无法得到文件名: {filePath}");
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
    }
}
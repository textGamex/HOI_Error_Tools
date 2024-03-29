﻿using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.Logic.Game;

public class GameResources
{
    public static IReadOnlyCollection<ErrorMessage> ErrorMessages => ErrorMessageCache;

    /// <summary>
    /// Key 为 建筑名称
    /// </summary>
    public IReadOnlyDictionary<string, BuildingInfo> BuildingInfoMap { get; set; }
    public IReadOnlySet<string> ResourcesType { get; private set; }
    public IReadOnlySet<string> RegisteredStateCategories { get; private set; }
    public IReadOnlySet<string> RegisteredCountriesTag { get; private set; }
    public IReadOnlySet<string> RegisteredIdeologies { get; private set; }
    public IReadOnlySet<string> RegisteredIdeas { get; private set; }
    //public IReadOnlySet<string> RegisteredEquipmentSet { get; }
    public IReadOnlySet<string> RegisteredTechnologiesSet { get; private set; }
    public IReadOnlySet<string> RegisteredAutonomousState { get; private set; }
    public IReadOnlySet<string> RegisteredCharacters { get; private set; }

    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    public IReadOnlySet<uint> RegisteredProvinces { get; private set; }

    /// <summary>
    /// 文件名, 不包含文件后缀
    /// </summary>
    public IReadOnlySet<string> RegisteredOobFileNames { get; private set; }

    private readonly GameResourcesPath _gameResourcesPath;
    private static readonly ConcurrentBag<ErrorMessage> ErrorMessageCache = new();
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public GameResources(string gameRootPath, string modRootPath) 
        : this(new GameResourcesPath(gameRootPath, modRootPath))
    {
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public GameResources(GameResourcesPath paths)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _gameResourcesPath = paths;
        var tasks = new[]
        {
            Task.Run(() => RegisteredProvinces = GetRegisteredProvinceSet()),
            Task.Run(() => BuildingInfoMap = GetRegisteredBuildings()),
            Task.Run(() => ResourcesType = GetResourcesType()),
            Task.Run(() => RegisteredStateCategories = GetRegisteredStateCategories()),
            Task.Run(() => RegisteredCountriesTag = GetCountriesTag()),
            Task.Run(() => RegisteredIdeologies = GetRegisteredIdeologies()),
            Task.Run(() => RegisteredTechnologiesSet = GetRegisteredTechnologies()),
            Task.Run(() => RegisteredAutonomousState = GetRegisteredAutonomousState()),
            Task.Run(() => RegisteredCharacters = GetRegisteredCharacters()),
            Task.Run(() => RegisteredOobFileNames = GetExistOobFiles()),
            Task.Run(() =>
            {
                var registeredIdeaTag = GetRegisteredIdeaTags();
                RegisteredIdeas = GetRegisteredIdeas(registeredIdeaTag.ToList());
            }), 
        };
        
        //RegisteredEquipmentSet = GetRegisteredEquipment();
        Task.WaitAll(tasks);
    }

    private IReadOnlySet<string> GetExistOobFiles()
    {
        var setBuild = new HashSet<string>(256);

        foreach (var path in _gameResourcesPath.OobFilesPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);
            setBuild.Add(fileName);
        }

        return setBuild.ToFrozenSet();
    }

    private IReadOnlySet<string> GetRegisteredCharacters()
    {
        return GetAllKeyOfNode(_gameResourcesPath.CharactersFilesPath, ScriptKeyWords.Characters);
    }

    private IReadOnlySet<string> GetRegisteredAutonomousState()
    {
        var set = new HashSet<string>(16);

        foreach (var path in _gameResourcesPath.AutonomousStateFilesPath)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            const string autonomyStateKeyword = "autonomy_state";
            if (rootNode.HasNot(autonomyStateKeyword))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(path));
                continue;
            }

            var autonomousStatesNode = rootNode.Child(autonomyStateKeyword).Value;
            if (autonomousStatesNode.HasNot(ScriptKeyWords.Id))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(
                    path, autonomousStatesNode.Key,ScriptKeyWords.Id));
                continue;
            }
            var idLeaf = autonomousStatesNode.Leafs(ScriptKeyWords.Id).First();
            if (!set.Add(idLeaf.ValueText))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateSingleFileError(
                    ErrorCode.UniqueValueIsRepeated, path, $"重复的 autonomy_state id 定义 '{idLeaf.Value}'"));
            }
        }

        return set.ToFrozenSet();
    }

    private IReadOnlySet<string> GetRegisteredTechnologies()
    {
        return GetAllKeyOfNode(_gameResourcesPath.TechnologyFilesPath, "technologies");
    }

    //private IReadOnlySet<string> GetRegisteredEquipment()
    //{
    //    return GetAllKeyOfNode(_gameResourcesPath.EquipmentFilesPath, "equipments");
    //}

    private static IReadOnlySet<string> GetAllKeyOfNode(IEnumerable<string> paths, string keyword)
    {
        var dictionary = new Dictionary<string, ParameterFileInfo>();
        foreach (var path in paths)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(keyword))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(path));
                continue;
            }
            var keywordNode = rootNode.Child(keyword).Value;

            foreach (var node in keywordNode.Nodes)
            {
                if (dictionary.TryGetValue(node.Key, out var fileInfo))
                {
                    var fileInfos = new ParameterFileInfo[]
                    {
                        fileInfo,
                        new(path, new Position(node.Position))
                    };
                    ErrorMessageCache.Add(new ErrorMessage(ErrorCode.UniqueValueIsRepeated, fileInfos, $"重复的 '{keyword}' 定义 '{node.Key}'", ErrorLevel.Error));
                }
                else
                {
                    dictionary.Add(node.Key, new ParameterFileInfo(path, new Position(node.Position)));
                }
            }
        }
        return dictionary.Keys.ToFrozenSet();
    }

    private IEnumerable<string> GetRegisteredIdeaTags()
    {
        var ideaTagList = new List<string>(64);
        const string ideaCategoriesKey = "idea_categories";
        const string characterSlotKey = "character_slot"; 
        const string slotKey = "slot";

        foreach (var path in _gameResourcesPath.IdeaTagsFilePath)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(ideaCategoriesKey))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(path));
                continue;
            }

            var ideaCategoriesNode = rootNode.Child(ideaCategoriesKey).Value;
            foreach (var node in ideaCategoriesNode.Nodes)
            {
                if (node.Has(slotKey) || node.Has(characterSlotKey))
                {
                    var slot = node.Leafs(slotKey);
                    ideaTagList.AddRange(node.Leafs(characterSlotKey).Union(slot).Select(leaf => leaf.ValueText));
                }
                else
                {
                    ideaTagList.Add(node.Key);
                }
            }
        }
        return ideaTagList.Distinct();
    }

    private IReadOnlySet<string> GetRegisteredIdeas(List<string> registeredIdeaTag)
    {
        var map = new Dictionary<string, ParameterFileInfo>(8);

        foreach (var path in _gameResourcesPath.IdeaFilesPath)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(ScriptKeyWords.Ideas))
            {
                continue;
            }

            var ideasNode = rootNode.Child(ScriptKeyWords.Ideas).Value;
            var subordinateMap = TryGetIdeas(ideasNode, path, registeredIdeaTag);
            MergeMap(map, subordinateMap);
        }

        return map.Select(item => item.Key).ToFrozenSet();
    }

    private static IReadOnlyDictionary<string, ParameterFileInfo> TryGetIdeas(Node rootNode, string filePath,
        List<string> keywords)
    {
        var map = new Dictionary<string, ParameterFileInfo>(64);
        foreach (var keyword in CollectionsMarshal.AsSpan(keywords))
        {
            if (rootNode.HasNot(keyword))
            {
                continue;
            }

            var node = rootNode.Child(keyword).Value;
            foreach (var item in node.Nodes)
            {
                if (map.TryGetValue(item.Key, out var value))
                {
                    var fileInfo = new List<ParameterFileInfo>()
                    {
                        new(value.FilePath, value.Position),
                        new(value.FilePath, new Position(item.Position))
                    };
                    ErrorMessageCache.Add(new ErrorMessage(
                        ErrorCode.UniqueValueIsRepeated,
                        fileInfo, $"重复的定义 Ideas '{item.Key}'", ErrorLevel.Error));
                }
                else
                {
                    map.Add(item.Key, new ParameterFileInfo(filePath, new Position(item.Position)));
                }
            }
        }
        return map;
    }

    private static void MergeMap(Dictionary<string, ParameterFileInfo> mainMap,
        IReadOnlyDictionary<string, ParameterFileInfo> secondaryMap)
    {
        foreach (var (ideaKey, fileInfo) in secondaryMap)
        {
            if (mainMap.TryGetValue(ideaKey, out var mainFilePath))
            {
                var fileInfoList = new List<ParameterFileInfo>()
                {
                    fileInfo,
                    mainFilePath
                };
                ErrorMessageCache.Add(new ErrorMessage(
                    ErrorCode.UniqueValueIsRepeated,
                    fileInfoList, $"重复的定义 Ideas '{ideaKey}'", ErrorLevel.Error));
            }
            else
            {
                mainMap.Add(ideaKey, fileInfo);
            }
        }
    }

    private IReadOnlySet<string> GetRegisteredIdeologies()
    {
        var set = new Dictionary<string, ParameterFileInfo>(64);

        foreach (var path in _gameResourcesPath.IdeologiesFilePath)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(ScriptKeyWords.Ideologies))
            {
                continue;
            }

            var ideologies = rootNode.Child(ScriptKeyWords.Ideologies).Value;
            foreach (var ideology in ideologies.Nodes)
            {
                if (set.TryGetValue(ideology.Key, out var ideologyPosition))
                {
                    var fileInfoList = new List<ParameterFileInfo>()
                    {
                        new (path, new Position(ideology.Position)),
                        new (ideologyPosition.FilePath, ideologyPosition.Position)
                    };
                    ErrorMessageCache.Add(new ErrorMessage(
                        ErrorCode.UniqueValueIsRepeated,
                        fileInfoList, "重复定义 ideology", ErrorLevel.Warn));
                }
                else
                {
                    set.Add(ideology.Key, new ParameterFileInfo(path, new Position(ideology.Position)));
                }
            }
        }

        return set.Keys.ToFrozenSet();
    }



    private IReadOnlySet<string> GetCountriesTag()
    {
        //TODO: 不能跨文件识别重复的国家标签
        var totalCountriesTag = new HashSet<string>(256);
        foreach (var path in _gameResourcesPath.CountriesTagFilePath)
        {
            var result = ParseFile(path);
            if (result is null)
            {
                continue;
            }

            totalCountriesTag.UnionWith(GetCountriesTagFromFile(path, result));
        }

        return totalCountriesTag.ToFrozenSet();
    }

    private static IReadOnlySet<string> GetCountriesTagFromFile(string filePath, Node result)
    {
        var separateBuilder = new HashSet<string>(128);
        foreach (var leaf in result.Leaves)
        {
            if (leaf.Key.Equals("dynamic_tags", StringComparison.OrdinalIgnoreCase) &&
                leaf.ValueText.Equals(ScriptKeyWords.Yes, StringComparison.OrdinalIgnoreCase))
            {
                separateBuilder.Clear();
                break;
            }

            if (!separateBuilder.Add(leaf.Key))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.UniqueValueIsRepeated,
                    filePath, new Position(leaf.Position), $"重复的国家标签 '{leaf.Key}'"));
            }
        }
        return separateBuilder;
    }

    private IReadOnlySet<string> GetRegisteredStateCategories()
    {
        var builder = new HashSet<string>(16);
        foreach (var path in _gameResourcesPath.StateCategoriesFilePath)
        {
            var result = ParseFile(path);
            if (result is null)
            {
                continue;
            }

            if (result.HasNot(Key.StateCategories))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(path));
                continue;
            }
            var stateCategoriesNode = result.Child(Key.StateCategories).Value;
            foreach (var item in stateCategoriesNode.Nodes)
            {
                builder.Add(item.Key);
            }
        }

        return builder.ToFrozenSet();
    }

    private IReadOnlyDictionary<string, BuildingInfo> GetRegisteredBuildings()
    {
        var builder = new Dictionary<string, BuildingInfo>();
        foreach (var filePath in _gameResourcesPath.BuildingsFilePathList)
        {
            var rootNode = ParseFile(filePath);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(ScriptKeyWords.Buildings))
            {
                ErrorMessageCache.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(filePath));
                continue;
            }
            var buildingsNode = rootNode.Child(ScriptKeyWords.Buildings).Value;
            foreach (var buildingInfo in ParseBuildingInfosToMap(filePath, buildingsNode))
            {
                //TODO: 重复时记录
                builder[buildingInfo.Key] = buildingInfo.Value;
            }
        }

        return builder.ToFrozenDictionary();
    }

    private static IDictionary<string, BuildingInfo> ParseBuildingInfosToMap(string filePath, Node buildingsNode)
    {
        var map = new Dictionary<string, BuildingInfo>(16);
        const string fuelSilo = "fuel_silo";
        foreach (var buildingNode in buildingsNode.Nodes)
        {
            var buildingTypeName = buildingNode.Key;

            // 排除特殊值, fuel_silo 类型没有最大等级
            if (buildingNode.Has(fuelSilo)
                && buildingNode.Leafs(fuelSilo).First().ValueText.Equals(ScriptKeyWords.Yes, StringComparison.OrdinalIgnoreCase))
            {
                map.Add(buildingTypeName, new BuildingInfo(buildingTypeName, 1));
                continue;
            }

            if (!TryParseMaxLevel(filePath, buildingNode, out var maxLevel))
            {
                continue;
            }
            var buildingInfo = new BuildingInfo(buildingTypeName, maxLevel);
            map.Add(buildingTypeName, buildingInfo);
        }

        return map;
    }

    private static bool TryParseMaxLevel(string filePath, Node buildingNode, out ushort maxLevel)
    {
        var maxLevelLeafs = buildingNode.Leafs(Key.MaxLevel).ToList();
        if (maxLevelLeafs.Count > 1)
        {
            ErrorMessageCache.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.UniqueValueIsRepeated,
                filePath, new Position(maxLevelLeafs[0].Position), "重复的 Key"));
        }

        var maxLevelLeaf = maxLevelLeafs[^1];
        if (ushort.TryParse(maxLevelLeaf.ValueText, out maxLevel))
        {
            return true;
        }
        ErrorMessageCache.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
            ErrorCode.ValueIsOutOfRange,
            filePath,
            new Position(maxLevelLeaf.Position),
            $"建筑物最大等级超过最大值 {ushort.MaxValue}",
            ErrorLevel.Warn));

        maxLevel = 0;
        return false;
    }

    /// <summary>
    /// 获得在 definition.csv 中所有的 Province ID
    /// </summary>
    /// <remarks>
    /// 所有 Province 在文件 Hearts of Iron IV\map\definition.csv 中定义
    /// </remarks>
    /// <returns></returns>
    private IReadOnlySet<uint> GetRegisteredProvinceSet()
    {
        var set = new HashSet<uint>(13257);
        using var reader = new StreamReader(_gameResourcesPath.ProvincesDefinitionFilePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        uint lineCount = 0;

        while (csv.Read())
        {
            ++lineCount;
            var line = csv.GetField(0);
            if (line is null)
            {
                Log.Warn(CultureInfo.InvariantCulture, "definition.csv 读取错误, 已跳过行数为 {Line} 的值", lineCount);
                continue;
            }
            string id = line[0..line.IndexOf(';')];
            set.Add(uint.Parse(id, CultureInfo.InvariantCulture));
        }

        // 去除 ID 为 0 的未知省份
        set.Remove(0);
        return set.ToFrozenSet();
    }

    private IReadOnlySet<string> GetResourcesType()
    {
        var builder = new HashSet<string>(16);
        foreach (var path in _gameResourcesPath.ResourcesTypeFilePathList)
        {
            var rootNode = ParseFile(path);
            if (rootNode is null)
            {
                continue;
            }

            if (rootNode.HasNot(ScriptKeyWords.Resources))
            {
                ErrorMessageCache.Add(
                    ErrorMessageFactory.CreateEmptyFileErrorMessage(path));
                continue;
            }

            var resourcesNode = rootNode.Child(ScriptKeyWords.Resources).Value;

            // 检查所有文件中是否有重复的资源类型
            //TODO: 实现在哪些文件中重复出现
            foreach (var type in ParseResourcesType(path, resourcesNode))
            {
                if (!builder.Contains(type))
                {
                    builder.Add(type);
                    continue;
                }
                ErrorMessageCache.Add(
                    ErrorMessageFactory.CreateSingleFileError(ErrorCode.DuplicateRegistration,
                        path, $"重复定义的资源类型: '{type}'"));
            }
        }
        return builder.ToFrozenSet();
    }

    private static IEnumerable<string> ParseResourcesType(string filePath, Node resourcesNode)
    {
        var set = new HashSet<string>(8);

        foreach (var node in resourcesNode.Nodes)
        {
            if (set.Contains(node.Key))
            {
                ErrorMessageCache.Add(
                    ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.DuplicateRegistration,
                        filePath, new Position(node.Position), $"重复定义的资源类型: '{node.Key}'", ErrorLevel.Warn));
                continue;
            }

            set.Add(node.Key);
        }
        return set;
    }

    /// <summary>
    /// 解析文件, 如果解析失败, 将错误信息添加到 <see cref="ErrorMessageCache"/>, 返回 <c>null</c>
    /// </summary>
    /// <param name="filePath">文件绝对路径</param>
    /// <returns>root Node</returns>
    private static Node? ParseFile(string filePath)
    {
        try
        {
            return ParseHelper.ParseFileToNode(ErrorMessageCache, filePath);
        }
        catch (IOException e)
        {
            Log.Error(e, "IO 错误, {Path} 解析失败", filePath);
            return null;
        }
    }

    public static void ClearErrorMessagesCache()
    {
        ErrorMessageCache.Clear();
    }

    private static class Key
    {
        public const string MaxLevel = "max_level";
        public const string StateCategories = "state_categories";
    }
}

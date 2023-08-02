using System.Collections.Concurrent;
using CsvHelper;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;
using NLog;

namespace HOI_Error_Tools.Logic;

public class GameResources
{
    public static IReadOnlyCollection<ErrorMessage> ErrorMessages => errorMessageCache;
    public IReadOnlySet<uint> RegisteredProvinceSet => _registeredProvinces;
    public IReadOnlyDictionary<string, BuildingInfo> BuildingInfoMap => _buildingInfos;
    public IImmutableSet<string> ResourcesType { get; }
    public IImmutableSet<string> RegisteredStateCategories { get; }
    public IImmutableSet<string> RegisteredCountriesTag { get; }

    private readonly ImmutableDictionary<string, BuildingInfo> _buildingInfos;
    private readonly ImmutableHashSet<uint> _registeredProvinces;
    private readonly GameResourcesPath _gameResourcesPath;

    private static readonly ConcurrentBag<ErrorMessage> errorMessageCache = new();
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public GameResources(GameResourcesPath paths)
    {
        _gameResourcesPath = paths;
        _registeredProvinces = ImmutableHashSet.CreateRange(GetRegisteredProvinceSet());
        _buildingInfos = GetRegisteredBuildings();
        ResourcesType = GetResourcesType();
        RegisteredStateCategories = GetRegisteredStateCategories();
        RegisteredCountriesTag = GetCountriesTag();
    }

    public GameResources(string gameRootPath, string modRootPath) : this(new GameResourcesPath(gameRootPath, modRootPath))
    {
    }

    private IImmutableSet<string> GetCountriesTag()
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var path in _gameResourcesPath.CountriesTagPath)
        {
            var parser = new CWToolsParser(path);
            if (parser.IsFailure)
            {
                errorMessageCache.Add(ErrorMessage.CreateSingleFileError(
                    path, "地块等级文件解析错误", ErrorLevel.Error));
                continue;
            }
            builder.UnionWith(GetCountriesTagFromFile(path, parser.GetResult()));
        }

        return builder.ToImmutableHashSet();
    }

    private static IReadOnlySet<string> GetCountriesTagFromFile(string filePath, Node result)
    {
        var separateBuilder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var leaf in result.Leaves)
        {
            if (leaf.Key == "dynamic_tags" && leaf.ValueText == "yes")
            {
                separateBuilder.Clear();
                break;
            }

            if (!separateBuilder.Add(leaf.Key))
            {
                errorMessageCache.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    filePath, new Position(leaf.Position), $"重复的国家标签 '{leaf.Key}'", ErrorLevel.Error));
            }
        }
        return separateBuilder.ToImmutableHashSet();
    }

    private IImmutableSet<string> GetRegisteredStateCategories()
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var path in _gameResourcesPath.StateCategoriesFilePath)
        {
            var parser = new CWToolsParser(path);
            if (parser.IsFailure)
            {
                errorMessageCache.Add(ErrorMessage.CreateSingleFileError(
                    path, "地块等级文件解析错误", ErrorLevel.Error));
                continue;
            }

            var result = parser.GetResult();
            if (result.HasNot(Key.StateCategories))
            {
                errorMessageCache.Add(ErrorMessage.CreateSingleFileError(
                    path, $"缺少 '{Key.StateCategories}' 关键字", ErrorLevel.Error));
                continue;
            }
            var stateCategoriesNode = result.Child(Key.StateCategories).Value;
            foreach (var item in stateCategoriesNode.Nodes)
            {
                builder.Add(item.Key);
            }
        }

        return builder.ToImmutableHashSet();
    }

    private ImmutableDictionary<string, BuildingInfo> GetRegisteredBuildings()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, BuildingInfo>();
        foreach (var filePath in _gameResourcesPath.BuildingsFilePathList)
        {
            var parser = new CWToolsParser(filePath);
            if (parser.IsFailure)
            {
                var error = parser.GetError();
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileErrorWithPosition(filePath, new Position(error), "解析错误", ErrorLevel.Error));
                continue;
            }

            var node = parser.GetResult();
            if (node.HasNot(ScriptKeyWords.Buildings))
            {
                errorMessageCache.Add(ErrorMessage.CreateSingleFileError(
                    filePath, $"缺少 '{ScriptKeyWords.Buildings}' 关键字", ErrorLevel.Error));
                continue;
            }
            var buildingsNode = node.Child(ScriptKeyWords.Buildings).Value;
            var map = ParseBuildingInfosToMap(filePath, buildingsNode);
            builder.AddRange(map);
        }
        
        return builder.ToImmutable();
    }

    private static IDictionary<string, BuildingInfo> ParseBuildingInfosToMap(string filePath, Node buildingsNode)
    {   
        var map = new Dictionary<string, BuildingInfo>();
        foreach (var buildingNode in buildingsNode.Nodes)
        {
            var buildingTypeName = buildingNode.Key;

            // 排除特殊值, fuel_silo 类型没有最大等级
            const string fuelSilo = "fuel_silo";            
            if (buildingNode.Has(fuelSilo) 
                && buildingNode.Leafs(fuelSilo).First().ValueText == "yes")
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
            errorMessageCache.Add(ErrorMessage.CreateSingleFileErrorWithPosition(filePath, new Position(maxLevelLeafs[0].Position), "重复的 Key", 
                ErrorLevel.Error));
        }

        var maxLevelLeaf = maxLevelLeafs.Last();
        if (ushort.TryParse(maxLevelLeaf.ValueText, out maxLevel))
        {
            return true;
        }
        errorMessageCache.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
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
    private IEnumerable<uint> GetRegisteredProvinceSet()
    {
        var set = new HashSet<uint>(13275);
        using var reader = new StreamReader(_gameResourcesPath.ProvincesDefinitionFilePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        while (csv.Read())
        {
            var line = csv.GetField(0) ?? string.Empty;            
            string id = line[0..line.IndexOf(';')];
            set.Add(uint.Parse(id));
        }

        // 去除 ID 为 0 的未知省份
        set.Remove(0);
        return set;
    }

    private IImmutableSet<string> GetResourcesType()
    {
        var builder = ImmutableHashSet.CreateBuilder<string>();
        foreach (var path in _gameResourcesPath.ResourcesTypeFilePathList)
        {
            var parser = new CWToolsParser(path);
            if (parser.IsFailure)
            {
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileErrorWithPosition(path, new Position(parser.GetError()), "解析错误", ErrorLevel.Error));
                continue;
            }

            var rootNode = parser.GetResult();
            if (rootNode.HasNot(ScriptKeyWords.Resources))
            {
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileError(path, "资源类型文件为空", ErrorLevel.Error));
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
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileError(path, $"重复定义的资源类型: '{type}'", ErrorLevel.Error));
            }
        }
        return builder.ToImmutable();
    }

    private static IEnumerable<string> ParseResourcesType(string filePath, Node resourcesNode)
    {
        var set = new HashSet<string>();

        foreach (var node in resourcesNode.Nodes)
        {
            if (set.Contains(node.Key))
            {
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileErrorWithPosition(filePath, new Position(node.Position), $"重复定义的资源类型: '{node.Key}'", 
                        ErrorLevel.Warn));
                continue;
            }

            set.Add(node.Key);
        }
        return set;
    }

    public static void ClearErrorMessagesCache()
    {
        errorMessageCache.Clear();
    }

    private static class Key
    {
        public const string MaxLevel = "max_level";
        public const string StateCategories = "state_categories";
    }
}

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
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using NLog;

namespace HOI_Error_Tools.Logic;

public class GameResources
{
    public static IReadOnlyCollection<ErrorMessage> ErrorMessages => errorMessageCache;
    public IReadOnlySet<uint> RegisteredProvinceSet => _registeredProvinces;
    public IImmutableDictionary<string, BuildingInfo> BuildingInfoMap => _buildingInfos;
    public IImmutableSet<string> ResourcesType { get; }
    public IImmutableSet<string> StateCategories { get; }

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
        StateCategories = GetStateCategories();
    }

    private IImmutableSet<string> GetStateCategories()
    {
        return null;
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
                    ErrorMessage.CreateSingleFileErrorWithPosition(filePath, new Position(error), "解析错误", ErrorType.ParseError));
                continue;
            }

            var node = parser.GetResult();
            var helper = new AnalyzeHelper(filePath, node);
            var errorMessages = helper.AssertKeywordExistsInCurrentNode(ScriptKeyWords.Buildings).ToList();
            if (errorMessages.Count != 0)
            {
                foreach (var errorMessage in errorMessages)
                {
                    errorMessageCache.Add(errorMessage);
                }
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
            if (buildingNode.Has("fuel_silo") 
                && buildingNode.Leafs("fuel_silo").First().ValueText == "yes")
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
                ErrorType.DuplicateValue));
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
            ErrorType.UnexpectedValue));

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
        var set = new HashSet<uint>(13257);
        using var reader = new StreamReader(_gameResourcesPath.ProvincesDefinitionFilePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        while (csv.Read())
        {
            var line = csv.GetField(0) ?? string.Empty;
            var id = line.Split(';')[0];
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
                    ErrorMessage.CreateSingleFileErrorWithPosition(path, new Position(parser.GetError()), "解析错误", ErrorType.ParseError));
                continue;
            }

            var rootNode = parser.GetResult();
            if (rootNode.HasNot(ScriptKeyWords.Resources))
            {
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileError(path, "资源类型文件为空", ErrorType.MissingKeyword));
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
                    ErrorMessage.CreateSingleFileError(path, $"重复定义的资源类型: '{type}'", ErrorType.DuplicateValue));
            }
        }
        return builder.ToImmutable();
    }

    private static IEnumerable<string> ParseResourcesType(string path, Node resourcesNode)
    {
        var set = new HashSet<string>();

        foreach (var node in resourcesNode.Nodes)
        {
            if (set.Contains(node.Key))
            {
                errorMessageCache.Add(
                    ErrorMessage.CreateSingleFileErrorWithPosition(path, new Position(node.Position), $"重复定义的资源类型: '{node.Key}'", 
                        ErrorType.DuplicateValue));
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
    }
}

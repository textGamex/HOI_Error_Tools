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

namespace HOI_Error_Tools.Logic;

public class GameResources
{
    public static IReadOnlyCollection<ErrorMessage> ErrorMessages => errorMessageCache;
    public IReadOnlySet<uint> RegisteredProvinceSet => _registeredProvinces;
    public IImmutableDictionary<string, BuildingInfo> BuildingInfoMap => _buildingInfos;

    private readonly ImmutableDictionary<string, BuildingInfo> _buildingInfos;
    private readonly ImmutableHashSet<uint> _registeredProvinces;
    private static readonly ConcurrentBag<ErrorMessage> errorMessageCache = new();

    public GameResources(GameResourcesPath paths)
    {
        _registeredProvinces = ImmutableHashSet.CreateRange(
            GetRegisteredProvinceSet(paths.ProvincesDefinitionFilePath));
        _buildingInfos = GetRegisteredBuildings(paths.BuildingsFilePathList);
    }

    private static ImmutableDictionary<string, BuildingInfo> GetRegisteredBuildings(IEnumerable<string> filesPath)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, BuildingInfo>();
        foreach (var filePath in filesPath)
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
        foreach (var child in buildingsNode.AllChildren)
        {
            if (!child.IsNodeC)
            {
                continue;
            }
            var buildingTypeName = child.node.Key;
            var helper = new AnalyzeHelper(filePath, child.node);

            var errorMessages = helper.AssertKeywordExistsInCurrentNodeAndWithPosition(Key.MaxLevel).ToList();
            if (errorMessages.Any())
            {
                foreach (var errorMessage in errorMessages)
                {
                    errorMessageCache.Add(errorMessage);
                }
                continue;
            }

            if (!TryParseMaxLevel(filePath, child.node, out var maxLevel))
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
    /// <param name="filePath">definition.csv 文件的绝对路径</param>
    /// <returns></returns>
    private static IEnumerable<uint> GetRegisteredProvinceSet(string filePath)
    {
        var set = new HashSet<uint>(13257);
        using var reader = new StreamReader(filePath, Encoding.UTF8);
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

    private static class Key
    {
        public const string MaxLevel = "max_level";
    }
}

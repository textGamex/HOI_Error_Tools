using CsvHelper;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using HOI_Error_Tools.Logic.Analyzers.Common;

namespace HOI_Error_Tools.Logic;

public class GameResources
{
    public IReadOnlySet<uint> RegisteredProvinceSet => _registeredProvinces;
    public IReadOnlyDictionary<string, BuildingInfo> BuildingInfoMap => _buildingInfos;

    private readonly ImmutableDictionary<string, BuildingInfo> _buildingInfos;
    private readonly ImmutableHashSet<uint> _registeredProvinces;

    public GameResources(GameResourcesPath paths)
    {
        _registeredProvinces = ImmutableHashSet.CreateRange(
            GetRegisteredProvinceSet(paths.ProvincesDefinitionFilePath));
        _buildingInfos = ImmutableDictionary.CreateRange(
            GetRegisteredBuildings(paths.BuildingsFilePathList));
    }

    private static Dictionary<string, BuildingInfo> GetRegisteredBuildings(IEnumerable<string> filePath)
    {
        return null;
    }

    /// <summary>
    /// 
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
}

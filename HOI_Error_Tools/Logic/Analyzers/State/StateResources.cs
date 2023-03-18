using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace HOI_Error_Tools.Logic.Analyzers.State;

/// <summary>
/// 保存着分析 State 文件所需要的资源
/// </summary>
public class StateResources
{
    public IReadOnlySet<uint> RegisteredProvinceSet => _registeredProvinces;
    private readonly HashSet<uint> _registeredProvinces;

    public StateResources(GameResourcesPath gameResourcesPath)
    {
        _registeredProvinces = GetRegisteredProvinceSet(gameResourcesPath.ProvincesDefinitionFilePath);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 所有 Province 在文件 Hearts of Iron IV\map\definition.csv 中定义
    /// </remarks>
    /// <param name="filePath">definition.csv 文件的绝对路径</param>
    /// <returns></returns>
    private static HashSet<uint> GetRegisteredProvinceSet(string filePath)
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
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;

    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    private readonly IReadOnlySet<uint> _registeredProvince;
    private readonly IImmutableDictionary<string, BuildingInfo> _registeredBuildings;
    private readonly IImmutableSet<string> _resourcesTypeSet;
    /// <summary>
    /// 已经分配的 Provinces, 用于检查重复分配错误
    /// </summary>
    private static readonly ConcurrentDictionary<uint, (Position Position, string FilePath)> ExistingProvinces = new();

    public StateFileAnalyzer(string filePath, GameResources resources)
    {
        _filePath = filePath;
        _registeredProvince = resources.RegisteredProvinceSet;
        _registeredBuildings = resources.BuildingInfoMap;
        _resourcesTypeSet = resources.ResourcesType;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var parser = new CWToolsParser(_filePath);
        var errorList = new List<ErrorMessage>();

        if (parser.IsFailure)
        {
            errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                _filePath, new Position(parser.GetError()), "解析错误", ErrorLevel.Error));
            return errorList;
        }

        var result = parser.GetResult();
        var stateModel = new StateModel(result);
        if (result.HasNot(ScriptKeyWords.State))
        {
            var errorMessage = ErrorMessage.CreateSingleFileError(_filePath, $"'{ScriptKeyWords.State}' 不存在", ErrorLevel.Error);
            errorList.Add(errorMessage);
            return errorList;
        }

        result = result.Child(ScriptKeyWords.State).Value;
        var helper = new AnalyzeHelper(_filePath, result);

        errorList.AddRange(helper.AssertKeywordExistsInCurrentNode(
            ScriptKeyWords.Id,
            ScriptKeyWords.StateCategory,
            ScriptKeyWords.Manpower,
            ScriptKeyWords.Name,
            ScriptKeyWords.History
        ));
        errorList.AddRange(helper.AssertKeywordExistsInChild(ScriptKeyWords.History, ScriptKeyWords.Owner));
        errorList.AddRange(AnalyzeProvinces(stateModel));
        errorList.AddRange(AnalyzeBuildings(result));
        errorList.AddRange(AssertResourcesTypeIsRegistered(stateModel));

        return errorList;
    }

    /// <summary>
    /// 如果 Provinces Key 不存在, 返回空集合
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="model"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AnalyzeProvinces(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        if (model.Provinces.Count == 0)
        {
            //TODO: 应该带上 provinces 块的位置
            errorList.Add(ErrorMessage.CreateSingleFileError(_filePath, "空的 provinces 块", ErrorLevel.Warn));
            return errorList;
        }

        var provinces = new List<(uint, Position)>();
        foreach (var (provinceIdText, position) in model.Provinces)
        {
            if (!uint.TryParse(provinceIdText, out var provinceId))
            {
                errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                                       _filePath, position, $"Province '{provinceIdText}' 无法转换为整数", ErrorLevel.Error));
                continue;
            }
            provinces.Add((provinceId, position));
        }
        errorList.AddRange(AssertProvincesIsRegistered(provinces));
        errorList.AddRange(AssertProvincesNotRepeat(provinces));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AssertProvincesIsRegistered(IEnumerable<(uint ProvinceId, Position Position)> provinces)
    {
        var errorList = new List<ErrorMessage>(16);
        foreach (var (provinceId, position) in provinces)
        {
            if (!_registeredProvince.Contains(provinceId))
            {
                errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"Province '{provinceId}' 未在 map\\definition.csv 文件中注册", ErrorLevel.Error));
            }
        }

        return errorList;
    }

    /// <summary>
    /// 检查 Provinces 是否重复分配
    /// </summary>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertProvincesNotRepeat(IEnumerable<(uint ProvinceId, Position Position)> provinces)
    {
        var errorList = new List<ErrorMessage>();
        foreach (var (provinceId, position) in provinces)
        {
            if (!ExistingProvinces.TryGetValue(provinceId, out var infoOfExistingValue))
            {
                if (!ExistingProvinces.TryAdd(provinceId, (position, _filePath)))
                {
                    throw new ArgumentException("ExistingProvinces 添加失败");
                }
                continue;
            }
            var fileInfo = new List<(string, Position)>
            {
                (_filePath, position),
                (infoOfExistingValue.FilePath, infoOfExistingValue.Position)
            };
            errorList.Add(new ErrorMessage(fileInfo, "Province 在文件中重复分配", ErrorLevel.Error));
        }
        
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeBuildings(Node result)
    {
        if (result.HasNot(ScriptKeyWords.History))
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var historyNode = result.Child(ScriptKeyWords.History).Value;
        if (historyNode.HasNot(ScriptKeyWords.Buildings))
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var buildingsNode = historyNode.Child(ScriptKeyWords.Buildings).Value;

        return AssertBuildingLevelWithinRange(buildingsNode);
    }

    private IEnumerable<ErrorMessage> AssertBuildingLevelWithinRange(Node buildingsNode)
    {
        var errorMessages = new List<ErrorMessage>();

        foreach (var leaf in buildingsNode.Leaves)
        {
            if (_registeredBuildings.TryGetValue(leaf.Key, out var buildingInfo))
            {
                if (!ushort.TryParse(leaf.ValueText, out var level))
                {
                    errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                        _filePath, new Position(leaf.Position), $"数值 '{leaf.ValueText}' 解析失败", ErrorLevel.Error));
                    continue;
                }

                if (level > buildingInfo.MaxLevel)
                {
                    errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                        _filePath,
                        new Position(leaf.Position),
                        $"建筑物等级: {level} 超过最大值: {buildingInfo.MaxLevel}",
                        ErrorLevel.Warn));
                    continue;
                }
            }
            else
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, new Position(leaf.Position), $"建筑物类型 '{leaf.Key}' 不存在", ErrorLevel.Error));
            }
        }

        return errorMessages;
    }

    /// <summary>
    /// 判断战略资源的类型是否存在
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertResourcesTypeIsRegistered(StateModel model)
    {
        if (model.Resources.Count == 0)
        {
            return Enumerable.Empty<ErrorMessage>();
        }
        var errorMessages = new List<ErrorMessage>();

        foreach (var (resourceType, amountText,position) in model.Resources)
        {
            if (!_resourcesTypeSet.Contains(resourceType))
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                                       _filePath, position, $"战略资源类型 '{resourceType}' 不存在", ErrorLevel.Error));
            }

            if (!uint.TryParse(amountText, out _))
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"战略资源数量 '{amountText}' 无法转换为整数", ErrorLevel.Error));
            }
        }

        return errorMessages;
    }

    public static void Clear()
    {
        ExistingProvinces.Clear();
    }
}
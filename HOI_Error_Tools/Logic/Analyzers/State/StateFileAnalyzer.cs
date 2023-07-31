using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace HOI_Error_Tools.Logic.Analyzers.State;

//TODO: 省份建筑的检查, 例如: 重复建筑, 不存在的建筑, 不存在的省份
public partial class StateFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;

    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    private readonly IReadOnlySet<uint> _registeredProvince;
    private readonly IImmutableDictionary<string, BuildingInfo> _registeredBuildings;
    private readonly IImmutableSet<string> _resourcesTypeSet;
    private readonly IImmutableSet<string> _registeredStateCategories;

    private static readonly ConcurrentDictionary<uint, (string FilePath, Position Position)> ExistingIds = new();

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
        _registeredStateCategories = resources.RegisteredStateCategories;
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
        if (stateModel.IsEmptyFile)
        {
            errorList.Add(ErrorMessage.CreateSingleFileError(_filePath, $"'{ScriptKeyWords.State}' 不存在", ErrorLevel.Error));
            return errorList;
        }

        errorList.AddRange(AnalyzeId(stateModel));
        errorList.AddRange(AnalyzeName(stateModel));
        errorList.AddRange(AnalyzeManpower(stateModel));
        errorList.AddRange(AnalyzeStateCategory(stateModel));
        errorList.AddRange(AnalyzeProvinces(stateModel));
        errorList.AddRange(AnalyzeBuildings(stateModel));
        errorList.AddRange(AssertResourcesTypeIsRegistered(stateModel));

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeId(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        if (!AssertExistKeyword(model.Ids, ScriptKeyWords.Id, out var errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var (idText, position) in model.Ids)
        {
            if (!uint.TryParse(idText, out var id))
            {
                errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"Id '{idText}' 无法转换为正整数", ErrorLevel.Error));
                continue;
            }

            if (ExistingIds.TryGetValue(id, out var existingIdOfFileInfo))
            {
                var fileInfo = new List<(string, Position)>()
                {
                    existingIdOfFileInfo,
                    (_filePath, position)
                };
                
                errorList.Add(new ErrorMessage(fileInfo, $"Id '{id}' 重复定义", ErrorLevel.Error));
            }
            else
            {
                bool result = ExistingIds.TryAdd(id, (_filePath, position));
                Debug.Assert(result, $"{nameof(ExistingIds)} 添加元素失败");
            }
        }

        if (!AssertKeywordIsOnly(model.Ids, ScriptKeyWords.Id, out errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
        }

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeName(StateModel model)
    {
        var errorList = new List<ErrorMessage>();

        if (!AssertExistKeyword(model.Names, ScriptKeyWords.Name, out var errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
            return errorList;
        }

        if (!AssertKeywordIsOnly(model.Names, ScriptKeyWords.Name, out errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeManpower(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        if (!AssertExistKeyword(model.Manpowers, ScriptKeyWords.Manpower, out var errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var (manpowerText, position) in model.Manpowers)
        {
            //TODO: manpower 的最大值是多少?
            if (!uint.TryParse(manpowerText, out _))
            {
                errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"Manpower '{manpowerText}' 无法转换为正整数", ErrorLevel.Error));
            }
        }

        if (!AssertKeywordIsOnly(model.Manpowers, ScriptKeyWords.Manpower, out errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
            return errorList;
        }

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeStateCategory(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        if (!AssertExistKeyword(model.StateCategories, ScriptKeyWords.StateCategory, out var errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var (type, position) in model.StateCategories)
        {
            if (!_registeredStateCategories.Contains(type))
            {
                errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"StateCategory 类型 '{type}' 未注册", ErrorLevel.Error));
            }
        }

        if (!AssertKeywordIsOnly(model.StateCategories, ScriptKeyWords.StateCategory, out errorMessage))
        {
            Debug.Assert(errorMessage != null, nameof(errorMessage) + " != null");
            errorList.Add(errorMessage);
        }
        return errorList;
    }

    private bool AssertExistKeyword<T>(IEnumerable<T> enumerable, string keyword, out ErrorMessage? errorMessage)
    {
        if (!enumerable.Any())
        {
            errorMessage = ErrorMessage.CreateSingleFileError(_filePath, $"缺少 '{keyword}' 关键字", ErrorLevel.Error);
            return false;
        }
        errorMessage = null;
        return true;
    }

    private bool AssertKeywordIsOnly(IReadOnlyCollection<(string, Position)> enumerable, string keyword,
        out ErrorMessage? errorMessage)
    {
        if (enumerable.Count > 1)
        {
            var errorFileInfo = new List<(string, Position)>();
            foreach (var (_, position) in enumerable)
            {
                errorFileInfo.Add((_filePath, position));
            }
            errorMessage = new ErrorMessage(errorFileInfo, $"重复的 '{keyword}' 关键字", ErrorLevel.Error);
            return false;
        }
        errorMessage = null;
        return true;
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
                                       _filePath, position, $"Province '{provinceIdText}' 无法转换为正整数", ErrorLevel.Error));
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

    private IEnumerable<ErrorMessage> AnalyzeBuildings(StateModel model)
    {
        return model.Buildings.Count == 0 ? Enumerable.Empty<ErrorMessage>() : AssertBuildingLevelWithinRange(model);
    }

    /// <summary>
    /// 判断 Buildings 是否合规 (建筑类型是否存在, 等级是否在范围内)
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertBuildingLevelWithinRange(StateModel model)
    {
        var errorMessages = new List<ErrorMessage>();

        foreach (var (buildingType, levelText, position) in model.Buildings)
        {
            if (!_registeredBuildings.TryGetValue(buildingType, out var buildingInfo))
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"建筑类型 '{buildingType}' 不存在", ErrorLevel.Error));
                continue;
            }

            if (!uint.TryParse(levelText, out var level))
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"建筑等级 '{levelText}' 无法转换为整数", ErrorLevel.Error));
                continue;
            }

            if (level > buildingInfo.MaxLevel)
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"建筑等级 '{level}' 超出范围 [{buildingInfo.MaxLevel}]", ErrorLevel.Error));
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

        foreach (var (resourceType, amountText, position) in model.Resources)
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
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public partial class StateFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;
    private readonly AnalyzerHelper _helper;
    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    private readonly IReadOnlySet<uint> _registeredProvince;

    /// <summary>
    /// Key 为 建筑名称
    /// </summary>
    private readonly IReadOnlyDictionary<string, BuildingInfo> _registeredBuildings;
    private readonly IReadOnlySet<string> _resourcesTypeSet;
    private readonly IReadOnlySet<string> _registeredStateCategories;
    private readonly IReadOnlySet<string> _registeredCountriesTag;
    private string FileName => Path.GetFileNameWithoutExtension(_filePath);

    private static readonly ConcurrentDictionary<uint, ParameterFileInfo> ExistingIds = new();

    /// <summary>
    /// 已经分配的 Provinces, 用于检查重复分配错误
    /// </summary>
    private static readonly ConcurrentDictionary<uint, ParameterFileInfo> ExistingProvinces = new();
        
    public StateFileAnalyzer(string filePath, GameResources resources) 
    {
        _filePath = filePath;
        _helper = new AnalyzerHelper(_filePath);
        _registeredProvince = resources.RegisteredProvinceSet;
        _registeredBuildings = resources.BuildingInfoMap;
        _resourcesTypeSet = resources.ResourcesType;
        _registeredStateCategories = resources.RegisteredStateCategories;
        _registeredCountriesTag = resources.RegisteredCountriesTag;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var parser = new CWToolsParser(_filePath);
        var errorList = new List<ErrorMessage>();

        if (parser.IsFailure)
        {
            errorList.Add(ErrorMessageFactory.CreateParseErrorMessage(
                _filePath, parser.GetError()));
            return errorList;
        }

        var result = parser.GetResult();
        var stateModel = new StateModel(result);
        if (stateModel.IsEmptyFile)
        {
            errorList.Add(ErrorMessageFactory.CreateSingleFileError(_filePath, $"'{ScriptKeyWords.State}' 不存在"));
            return errorList;
        }

        var provinceInStateSet = stateModel.Provinces
            .SelectMany(leafNode => leafNode.LeafValueContents.Select(leafValue => leafValue.ValueText))
            .ToHashSet();

        errorList.AddRange(AnalyzeId(stateModel));
        errorList.AddRange(AnalyzeName(stateModel));
        errorList.AddRange(AnalyzeManpower(stateModel));
        errorList.AddRange(AnalyzeStateCategory(stateModel));
        errorList.AddRange(AnalyzeProvinces(stateModel));
        errorList.AddRange(AnalyzeBuildingsByProvince(stateModel, provinceInStateSet));
        errorList.AddRange(AnalyzeVictoryPoints(stateModel, provinceInStateSet));
        errorList.AddRange(AnalyzeBuildings(stateModel));
        errorList.AddRange(AnalyzeOwner(stateModel));
        errorList.AddRange(AnalyzeHasCoreTags(stateModel));
        errorList.AddRange(AssertResourcesTypeIsRegistered(stateModel));

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeVictoryPoints(StateModel model, IReadOnlySet<string> provinceInStateSet)
    {
        var errorList = new List<ErrorMessage>();
        
        foreach (var node in model.VictoryPointNodes)
        {
            var victoryPoints = node.LeafValueContents.ToList();
            if (victoryPoints.Count != 2)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, node.Position, "VictoryPoints 格式不正确"));
                continue;
            }

            var victoryPointValue = victoryPoints[1];
            if (!victoryPointValue.Value.IsNumber || victoryPointValue.Value.IsNegativeNumber)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, victoryPointValue.Position, $"胜利点价值 '{victoryPointValue.ValueText}' 无法转换为正整数"));
            }

            var provinceIdLeafValue = victoryPoints[0];
            if (!provinceIdLeafValue.Value.IsNumber || provinceIdLeafValue.Value.IsNegativeNumber)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, provinceIdLeafValue.Position, $"ProvinceId '{provinceIdLeafValue.ValueText}' 无法转换为正整数"));
                continue;
            }

            if (!provinceInStateSet.Contains(provinceIdLeafValue.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, 
                    provinceIdLeafValue.Position,
                    $"Province '{provinceIdLeafValue.ValueText}' 未分配在 State '{FileName}' 中, 但却在此地有 VictoryPoints", 
                    ErrorLevel.Warn));
            }
        }

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeBuildingsByProvince(StateModel model, IReadOnlySet<string> provinceInStateSet)
    {
        var errorList = new List<ErrorMessage>();

        foreach (var (provinceIdText, buildings, position) in model.BuildingsByProvince)
        {
            errorList.AddRange(AssertBuildingLevelWithinRange(buildings));
            if (!uint.TryParse(provinceIdText, out var provinceId))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"ProvinceId '{provinceIdText}' 无法转换为正整数"));
                continue;
            }

            if (!_registeredProvince.Contains(provinceId))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"ProvinceId '{provinceId}' 未注册"));
            }

            if (!provinceInStateSet.Contains(provinceIdText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"Province '{provinceId}' 未分配在 State '{FileName}' 中, 但却在此地有 Province 建筑"));
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeHasCoreTags(StateModel model)
    {
        var errorList = new List<ErrorMessage>(3);

        foreach (var leaf in model.HasCoreTags)
        {
            var tag = leaf.ValueText;
            if (!_registeredCountriesTag.Contains(tag))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, leaf.Position, $"国家Tag '{tag}' 未注册"));
            }
        }

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeOwner(StateModel model)
    {
        var errorList = new List<ErrorMessage>(5);

        var error = _helper.AssertExistKeyword(model.Owners, ScriptKeyWords.Owner);
        if (error is not null)
        {
            errorList.Add(error);
            return errorList;
        }

        foreach (var ownerLeaf in model.Owners)
        {
            var owner = ownerLeaf.ValueText;
            if (!_registeredCountriesTag.Contains(owner))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, ownerLeaf.Position, $"国家Tag '{owner}' 未注册"));
            }
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.Owners, ScriptKeyWords.Owner));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeId(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        var errorMessage = _helper.AssertExistKeyword(model.Ids, ScriptKeyWords.Id);

        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var idLeaf in model.Ids)
        {
            var idText = idLeaf.ValueText;
            if (!uint.TryParse(idText, out var id))
            {
                errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(_filePath, idLeaf));
                continue;
            }

            if (ExistingIds.TryGetValue(id, out var existingIdOfFileInfo))
            {
                var fileInfo = new List<ParameterFileInfo>()
                {
                    existingIdOfFileInfo,
                    new (_filePath, idLeaf.Position)
                };
                
                errorList.Add(new ErrorMessage(fileInfo, $"Id '{id}' 重复定义", ErrorLevel.Error));
            }
            else
            {
                bool result = ExistingIds.TryAdd(id, new ParameterFileInfo(_filePath, idLeaf.Position));
                Debug.Assert(result, $"{nameof(ExistingIds)} 添加元素失败");
            }
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.Ids, ScriptKeyWords.Id));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeName(StateModel model)
    {
        var errorList = new List<ErrorMessage>();

        var errorMessage = _helper.AssertExistKeyword(model.Names, ScriptKeyWords.Name);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.Names, ScriptKeyWords.Name));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeManpower(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        var errorMessage = _helper.AssertExistKeyword(model.Manpowers, ScriptKeyWords.Manpower);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var manpowerLeaf in model.Manpowers)
        {
            var manpowerText = manpowerLeaf.ValueText;
            //TODO: manpower 的最大值是多少?
            if (!manpowerLeaf.Value.IsInt)
            {
                errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(_filePath, manpowerLeaf));
            }
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.Manpowers, ScriptKeyWords.Manpower));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeStateCategory(StateModel model)
    {
        var errorList = new List<ErrorMessage>();
        var errorMessage = _helper.AssertExistKeyword(model.StateCategories, ScriptKeyWords.StateCategory);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var stateCategoryLeaf in model.StateCategories)
        {
            var type = stateCategoryLeaf.ValueText;
            if (!_registeredStateCategories.Contains(type))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, stateCategoryLeaf.Position, $"StateCategory 类型 '{type}' 未注册"));
            }
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.StateCategories, ScriptKeyWords.StateCategory));
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
            errorList.Add(ErrorMessageFactory.CreateSingleFileError(_filePath, "空的 provinces 块", ErrorLevel.Warn));
            return errorList;
        }

        var provinces = new List<(uint, Position)>(16);
        foreach (var leafValueNode in model.Provinces)
        {
            foreach (var provinceIdLeafValue in leafValueNode.LeafValueContents)
            {
                var provinceIdText = provinceIdLeafValue.ValueText;
                if (!provinceIdLeafValue.Value.IsInt)
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, provinceIdLeafValue.Position, $"Province '{provinceIdText}' 无法转换为正整数"));
                    continue;
                }
                provinces.Add((uint.Parse(provinceIdText), provinceIdLeafValue.Position));
            }
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
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, position, $"Province '{provinceId}' 未在 map\\definition.csv 文件中注册"));
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
                if (!ExistingProvinces.TryAdd(provinceId, new ParameterFileInfo(_filePath, position)))
                {
                    throw new ArgumentException("ExistingProvinces 添加失败");
                }
                continue;
            }
            var fileInfo = new List<ParameterFileInfo>
            {
                new (_filePath, position),
                new (infoOfExistingValue.FilePath, infoOfExistingValue.Position)
            };
            errorList.Add(new ErrorMessage(fileInfo, $"Province '{provinceId}' 在不同文件中重复分配", ErrorLevel.Error));
        }
        
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeBuildings(StateModel model)
    {
        return model.Buildings.Count == 0 ? Enumerable.Empty<ErrorMessage>() : AssertBuildingLevelWithinRange(model.Buildings);
    }

    /// <summary>
    /// 判断 Buildings 是否合规 (类型是否存在, 等级是否在范围内, 是否重复声明)
    /// </summary>
    /// <param name="buildings"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertBuildingLevelWithinRange(IReadOnlyList<LeafContent> buildings)
    {
        var errorMessages = new List<ErrorMessage>();
        //TODO: 待优化
        var existingBuildings = new Dictionary<string, List<Position>>();

        foreach (var building in buildings)
        {
            var buildingType = building.Key;
            var levelText = building.ValueText;
            if (existingBuildings.TryGetValue(buildingType, out var list))
            {
                list.Add(building.Position);
            }
            else
            {
                existingBuildings.Add(buildingType, new List<Position>() { building.Position });
            }

            // 建筑类型和建筑等级是否为整数可以一起判断
            if (!_registeredBuildings.TryGetValue(buildingType, out var buildingInfo))
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, building.Position, $"建筑类型 '{buildingType}' 不存在"));
            }

            if (!uint.TryParse(levelText, out var level))
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, building.Position, $"建筑等级 '{levelText}' 无法转换为整数"));
                continue;
            }

            // 检测 buildingInfo 是否为 null 是因为建筑类型和建筑等级是互不干扰的两项检测.
            // 当建筑类型不存在时, buildingInfo 为 null, 但是 levelText 仍然有可能为整数, 仍然可以进行等级合法性检测.
            if (buildingInfo != null && level > buildingInfo.MaxLevel)
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, building.Position, $"建筑物 '{buildingType}' 等级 [{level}] 超出范围 [{buildingInfo.MaxLevel}]"));
            }
        }
        errorMessages.AddRange(GetErrorOfRepeatedBuildingsType(existingBuildings));
        return errorMessages;
    }

    private IEnumerable<ErrorMessage> GetErrorOfRepeatedBuildingsType(Dictionary<string, List<Position>> existingBuildings)
    {
        var errorMessages = new List<ErrorMessage>();
        foreach (var (buildingType, positionList) in existingBuildings)
        {
            if (positionList.Count < 2)
            {
                continue;
            }
            var fileInfo = positionList.Select(position => new ParameterFileInfo(_filePath, position));
            errorMessages.Add(new ErrorMessage(fileInfo, $"重复声明的建筑物 '{buildingType}'", ErrorLevel.Error));
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
        if (model.ResourceNodes.Count == 0)
        {
            return Enumerable.Empty<ErrorMessage>();
        }
        var errorList = new List<ErrorMessage>();

        if (model.ResourceNodes.Count > 1)
        {
            errorList.Add(ErrorMessageFactory.CreateSingleFileError(_filePath, "战略资源应在同一个块中声明", ErrorLevel.Tip));
        }

        foreach (var resourceNode in model.ResourceNodes)
        {
            foreach (var resourceLeaf in resourceNode.Leaves)
            {
                var resourceType = resourceLeaf.Key;
                var amount = resourceLeaf.ValueText;
                if (!_resourcesTypeSet.Contains(resourceType))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, resourceLeaf.Position, $"战略资源类型 '{resourceType}' 不存在"));
                }

                if (!resourceLeaf.Value.IsNumber || resourceLeaf.Value.IsNegativeNumber)
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, resourceLeaf.Position, $"战略资源数量 '{amount}' 无法转换为非负整数"));
                }
            }
        }
        return errorList;
    }

    public static void Clear()
    {
        ExistingProvinces.Clear();
        ExistingIds.Clear();
    }
}
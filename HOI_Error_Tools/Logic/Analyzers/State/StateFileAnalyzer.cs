using System;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.Game;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public sealed partial class StateFileAnalyzer : AnalyzerBase
{
    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    private readonly IReadOnlySet<uint> _registeredProvinces;

    /// <summary>
    /// Key 为 建筑名称
    /// </summary>
    private readonly IReadOnlyDictionary<string, BuildingInfo> _registeredBuildings;
    private readonly IReadOnlySet<string> _resourcesTypeSet;
    private readonly IReadOnlySet<string> _registeredStateCategories;
    private readonly IReadOnlySet<string> _registeredCountriesTag;
    private readonly List<ErrorMessage> _errorList = new(5);

    private static readonly ConcurrentDictionary<uint, ParameterFileInfo> ExistingIds = new();
    /// <summary>
    /// 已经分配的 Provinces, 用于检查重复分配错误
    /// </summary>
    private static readonly ConcurrentDictionary<uint, ParameterFileInfo> ExistingProvinces = new();
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();
    
    public StateFileAnalyzer(string filePath, GameResources resources) : base(filePath)
    {
        _registeredProvinces = resources.RegisteredProvinceSet;
        _registeredBuildings = resources.BuildingInfoMap;
        _resourcesTypeSet = resources.ResourcesType;
        _registeredStateCategories = resources.RegisteredStateCategories;
        _registeredCountriesTag = resources.RegisteredCountriesTag;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        Node? rootNode = null;
        try
        {
            rootNode = ParseHelper.ParseFileToNode(_errorList, FilePath);
        }
        catch (FileNotFoundException e)
        {
            Log.Error(e, "解析的文件不存在, path: {Path}", FilePath);
        }
        catch (IOException e)
        {
            Log.Error(e, "发生 IO 错误, path: {Path}", FilePath);
        }

        if (rootNode is null)
        {
            return _errorList;
        }

        var stateModel = new StateModel(rootNode);
        if (stateModel.IsEmptyFile)
        {
            _errorList.Add(ErrorMessageFactory.CreateEmptyFileErrorMessage(FilePath));
            return _errorList;
        }

        var provinceInStateSet = stateModel.ProvinceNodes
            .SelectMany(leafNode => leafNode.LeafValueContents.Select(leafValue => leafValue.ValueText))
            .ToHashSet();

        AnalyzeId(stateModel);
        AnalyzeName(stateModel);
        AnalyzeManpower(stateModel);
        AnalyzeStateCategory(stateModel);
        AnalyzeProvinces(stateModel);
        AnalyzeBuildingsByProvince(stateModel, provinceInStateSet);
        AnalyzeVictoryPoints(stateModel, provinceInStateSet);
        AnalyzeBuildings(stateModel);
        AnalyzeOwner(stateModel);
        AnalyzeOwnCoreTags(stateModel);
        AnalyzeControllerTags(stateModel);
        AnalyzeClaimCountryTags(stateModel);
        AnalyzeLocalSupplies(stateModel);
        AssertResourcesTypeIsRegistered(stateModel);

        return _errorList;
    }

    private void AnalyzeLocalSupplies(StateModel stateModel)
    {
        foreach (var leaf in stateModel.LocalSupplies)
        {
            if (!leaf.Value.IsNumber)
            {
                _errorList.Add(
                    ErrorMessageFactory.CreateInvalidValueErrorMessage(FilePath, leaf, "number"));
            }
        }
        _errorList.AddRange(Helper.AssertKeywordIsOnly(stateModel.LocalSupplies));
    }

    private void AnalyzeClaimCountryTags(StateModel stateModel)
    {
        CheckCountryTagsUniqueness(stateModel.ClaimCountryTags);
        foreach (var leaf in stateModel.ClaimCountryTags)
        {
            CheckCountryTagValidity(leaf.ValueText, leaf.Position);
        }
    }

    private void AnalyzeControllerTags(StateModel stateModel)
    {
        if (stateModel.ControllerTags.Count == 0)
        {
            return;
        }

        _errorList.AddRange(Helper.AssertKeywordIsOnly(stateModel.ControllerTags));
        foreach (var controllerTag in stateModel.ControllerTags)
        {
            CheckCountryTagValidity(controllerTag.ValueText, controllerTag.Position);
        }
    }

    /// <summary>
    /// 检查国家标签是否合法, 如果不合法则添加 <see cref="ErrorMessage"/> 到字段 <c>_errorList</c>
    /// </summary>
    /// <param name="countryTag">国家标签</param>
    /// <param name="position"></param>
    private void CheckCountryTagValidity(string countryTag, Position position)
    {
        if (countryTag.Length != 3)
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.CountryTagFormatIsInvalid, FilePath, position, $"Country Tag '{countryTag}' 格式错误"));
        }
        
        if (!_registeredCountriesTag.Contains(countryTag))
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.CountryTagNotExists,
                FilePath, position, $"国家 Tag '{countryTag}' 未注册"));
        }
    }

    private void AnalyzeId(StateModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.Ids, ScriptKeyWords.Id);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
        }

        foreach (var idLeaf in model.Ids)
        {
            var idText = idLeaf.ValueText;
            if (!uint.TryParse(idText, out var id))
            {
                _errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(FilePath, idLeaf));
                continue;
            }

            if (ExistingIds.TryGetValue(id, out var existingIdOfFileInfo))
            {
                var fileInfo = new List<ParameterFileInfo>()
                {
                    existingIdOfFileInfo,
                    new (FilePath, idLeaf.Position)
                };

                _errorList.Add(new ErrorMessage(
                    ErrorCode.UniqueValueIsRepeated, fileInfo, $"相同的Id '{id}' 在不同的文件中使用", ErrorLevel.Error));
            }
            else
            {
                var info = new ParameterFileInfo(FilePath, idLeaf.Position);
                if (!ExistingIds.TryAdd(id, info))
                {
                    Log.Warn("{Id}={Info} 添加失败", id, info);
                }
            }
        }
        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.Ids));
    }

    private void AnalyzeVictoryPoints(StateModel model, IReadOnlySet<string> provinceInStateSet)
    {
        foreach (var node in model.VictoryPointNodes)
        {
            var victoryPoints = node.LeafValueContents.ToList();
            if (victoryPoints.Count != 2)
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.VictoryPointsFormatIsInvalid, FilePath, node.Position, "VictoryPoints 格式不正确"));
                continue;
            }

            var victoryPointValue = victoryPoints[1];
            if (!victoryPointValue.Value.IsNumber || victoryPointValue.Value.IsNegativeNumber)
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.FailedStringToIntError,
                    FilePath, victoryPointValue.Position, $"胜利点价值 '{victoryPointValue.ValueText}' 无法转换为正整数"));
            }

            var provinceIdLeafValue = victoryPoints[0];
            if (!provinceIdLeafValue.Value.IsNumber || provinceIdLeafValue.Value.IsNegativeNumber)
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.FailedStringToIntError,
                    FilePath, provinceIdLeafValue.Position, $"ProvinceId '{provinceIdLeafValue.ValueText}' 无法转换为正整数"));
                continue;
            }

            if (!provinceInStateSet.Contains(provinceIdLeafValue.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.ProvinceNotExistsInStateFile, FilePath, provinceIdLeafValue.Position,
                    $"Province '{provinceIdLeafValue.ValueText}' 未分配在 State '{FileName}' 中, 但却在此地有 VictoryPoints",
                    ErrorLevel.Warn));
            }
            CheckProvinceIsExisting(uint.Parse(provinceIdLeafValue.ValueText, CultureInfo.InvariantCulture),
                provinceIdLeafValue.Position);
        }
    }

    private void AnalyzeBuildingsByProvince(StateModel model, IReadOnlySet<string> provinceInStateSet)
    {
        foreach (var provinceNode in model.BuildingsByProvince)
        {
            var provinceIdText = provinceNode.Key;
            var buildings = provinceNode.Leaves;
            _errorList.AddRange(AssertBuildingLevelWithinRange(buildings));
            if (!uint.TryParse(provinceIdText, out var provinceId))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.FailedStringToIntError,
                    FilePath, provinceNode.Position, $"Province '{provinceIdText}' 无法转换为正整数"));
                continue;
            }

            CheckProvinceIsExisting(provinceId, provinceNode.Position);

            if (!provinceInStateSet.Contains(provinceIdText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.ProvinceNotExistsInStateFile,
                    FilePath, provinceNode.Position, 
                    $"Province '{provinceId}' 未分配在 State '{FileName}' 中, 但却在此地有 Province 建筑"));
            }
        }
    }

    private void CheckProvinceIsExisting(uint provinceId, Position position)
    {
        if (!_registeredProvinces.Contains(provinceId))
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.ProvinceNotExistsInDefinitionCsvFile,
                FilePath, position, $"Province '{provinceId}' 未在 'definition.csv' 文件中注册"));
        }
    }

    private void AnalyzeOwner(StateModel model)
    {
        var error = Helper.AssertExistKeyword(model.Owners, ScriptKeyWords.Owner);
        if (error is not null)
        {
            _errorList.Add(error);
            return;
        }

        foreach (var ownerLeaf in model.Owners)
        {
            var ownerTag = ownerLeaf.ValueText;
            CheckCountryTagValidity(ownerTag, ownerLeaf.Position);
        }
        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.Owners));
    }

    private void AnalyzeOwnCoreTags(StateModel model)
    {
        CheckCountryTagsUniqueness(model.OwnCoreTags);
        foreach (var leaf in model.OwnCoreTags)
        {
            var tag = leaf.ValueText;
            CheckCountryTagValidity(tag, leaf.Position);
        }
    }

    private void CheckCountryTagsUniqueness(IReadOnlyCollection<LeafContent> leaves)
    {
        _errorList.AddRange(Helper.AssertValueIsOnly(leaves, tag => $"文件 '{FileName}' 中重复添加的 {tag} 地区核心", 
            leaf => leaf.ValueText));
    }

    private void AnalyzeName(StateModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.Names, ScriptKeyWords.Name);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
        }

        _errorList.AddRange(Helper.AssertValueTypeIsExpected(model.Names, Value.Types.String));
        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.Names));
    }

    private void AnalyzeManpower(StateModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.Manpowers, ScriptKeyWords.Manpower);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
        }

        foreach (var manpowerLeaf in model.Manpowers)
        {
            //TODO: manpower 的最大值是多少?
            if (!manpowerLeaf.Value.IsInt)
            {
                _errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(FilePath, manpowerLeaf));
            }
        }

        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.Manpowers));
    }

    private void AnalyzeStateCategory(StateModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.StateCategories, ScriptKeyWords.StateCategory);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
        }

        foreach (var stateCategoryLeaf in model.StateCategories)
        {
            var type = stateCategoryLeaf.ValueText;
            if (!_registeredStateCategories.Contains(type))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.StateCategoryNotExists,
                    FilePath, stateCategoryLeaf.Position, $"StateCategory 类型 '{type}' 未注册"));
            }
            _errorList.AddRange(Helper.AssertValueTypeIsExpected(stateCategoryLeaf, Value.Types.String));
        }

        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.StateCategories));
    }

    /// <summary>
    /// 如果 Provinces Key 不存在, 返回空集合
    /// </summary>
    /// <remarks>
    /// </remarks>
    /// <param name="model"></param>
    /// <returns></returns>
    private void AnalyzeProvinces(StateModel model)
    {
        if (model.ProvinceNodes.Count == 0)
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileError(
                ErrorCode.EmptyProvincesNode, FilePath,  $"State 文件 '{FileName}' 未分配 province", ErrorLevel.Warn));
            return;
        }

        var provinces = new List<(uint, Position)>(16);
        foreach (var provinceNode in model.ProvinceNodes)
        {
            var provinceIds = provinceNode.LeafValueContents;
            if (!provinceIds.Any())
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.EmptyProvincesNode, FilePath, provinceNode.Position,
                    $"文件 '{FileName}' 存在空的 provinces 节点", ErrorLevel.Warn));
                continue;
            }

            foreach (var provinceIdLeafValue in provinceIds)
            {
                var provinceIdText = provinceIdLeafValue.ValueText;
                if (!provinceIdLeafValue.Value.IsInt)
                {
                    _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.FailedStringToIntError,
                        FilePath, provinceIdLeafValue.Position, $"Province '{provinceIdText}' 无法转换为正整数"));
                    continue;
                }
                provinces.Add((uint.Parse(provinceIdText, CultureInfo.InvariantCulture), provinceIdLeafValue.Position));
            }
        }
        AssertProvincesIsRegistered(provinces);
        AssertProvincesNotRepeat(provinces);
    }

    private void AssertProvincesIsRegistered(IEnumerable<(uint ProvinceId, Position Position)> provinces)
    {
        foreach (var (provinceId, position) in provinces)
        {
            if (!_registeredProvinces.Contains(provinceId))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.ProvinceNotExistsInDefinitionCsvFile,
                    FilePath, position, $"Province '{provinceId}' 未在 map\\definition.csv 文件中注册"));
            }
        }
    }

    /// <summary>
    /// 检查 Provinces 是否重复分配
    /// </summary>
    /// <returns></returns>
    private void AssertProvincesNotRepeat(IEnumerable<(uint ProvinceId, Position Position)> provinces)
    {
        foreach (var (provinceId, position) in provinces)
        {
            if (!ExistingProvinces.TryGetValue(provinceId, out var infoOfExistingValue))
            {
                if (!ExistingProvinces.TryAdd(provinceId, new ParameterFileInfo(FilePath, position)))
                { 
                    Log.Warn(CultureInfo.InvariantCulture,
                        "Province {Province} 向 {VarName} 添加失败", provinceId, nameof(ExistingProvinces));
                }
                continue;
            }
            
            var fileInfo = new List<ParameterFileInfo>
            {
                new (FilePath, position),
                new (infoOfExistingValue.FilePath, infoOfExistingValue.Position)
            };
            _errorList.Add(new ErrorMessage(ErrorCode.UniqueValueIsRepeated, fileInfo, 
                $"Province '{provinceId}' 在不同文件中重复分配", ErrorLevel.Error));
        }
    }

    private void AnalyzeBuildings(StateModel model)
    {
        if (model.BuildingNodes.Count == 0)
        {
            return;
        }

        foreach (var building in model.BuildingNodes)
        {
            _errorList.AddRange(AssertBuildingLevelWithinRange(building.Leaves));
        }
    }

    /// <summary>
    /// 判断 Buildings 是否合规 (类型是否存在, 等级是否在范围内, 是否重复声明)
    /// </summary>
    /// <param name="buildings"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertBuildingLevelWithinRange(IEnumerable<LeafContent> buildings)
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
                existingBuildings.Add(buildingType, new List<Position>{ building.Position });
            }

            // 建筑类型和建筑等级是否为整数可以一起判断
            if (!_registeredBuildings.TryGetValue(buildingType, out var buildingInfo))
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, building.Position, $"建筑类型 '{buildingType}' 不存在"));
            }
            if (!uint.TryParse(levelText, out var level))
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.FailedStringToIntError,
                    FilePath, building.Position, $"建筑等级 '{levelText}' 无法转换为整数"));
                continue;
            }

            // 检测 buildingInfo 是否为 null 是因为建筑类型和建筑等级是互不干扰的两项检测.
            // 当建筑类型不存在时, buildingInfo 为 null, 但是 levelText 仍然有可能为整数, 仍然可以进行等级合法性检测.
            if (buildingInfo != null && level > buildingInfo.MaxLevel)
            {
                errorMessages.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                   ErrorCode.ValueIsOutOfRange,
                   FilePath, building.Position, $"建筑物 '{buildingType}' 等级 [{level}] 超出范围 [{buildingInfo.MaxLevel}]")); 
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
            var fileInfo = positionList.Select(position => new ParameterFileInfo(FilePath, position));
            errorMessages.Add(new ErrorMessage(ErrorCode.DuplicateRegistration,
                fileInfo, $"重复声明的建筑物 '{buildingType}'", ErrorLevel.Warn));
        }
        return errorMessages;
    }

    /// <summary>
    /// 判断战略资源的类型是否存在
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    private void AssertResourcesTypeIsRegistered(StateModel model)
    {
        if (model.ResourceNodes.Count == 0)
        {
            return;
        }

        if (model.ResourceNodes.Count > 1)
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileError(
                ErrorCode.ResourcesNodeNotOnly, FilePath, "战略资源应在同一个块中声明", ErrorLevel.Tip));
        }

        var resourceTypes = new Dictionary<string, ParameterFileInfo>(model.ResourceNodes.Count);
        foreach (var resourceNode in model.ResourceNodes)
        {
            foreach (var resourceLeaf in resourceNode.Leaves)
            {
                var resourceType = resourceLeaf.Key;
                var amount = resourceLeaf.ValueText;
                
                // 检查 resourceType 是否重复声明
                if (resourceTypes.TryGetValue(resourceType, out var fileInfo))
                {
                    var fileInfos = new[]
                    {
                        fileInfo,
                        new ParameterFileInfo(FilePath, resourceLeaf.Position)
                    };
                    _errorList.Add(new ErrorMessage(ErrorCode.UniqueValueIsRepeated, fileInfos,
                        $"文件 '{FileName}' 战略资源 '{resourceType}' 重复声明", ErrorLevel.Warn));
                }
                else
                {
                    resourceTypes[resourceType] = new ParameterFileInfo(FilePath, resourceLeaf.Position);
                }

                CheckResourceIsExisting(resourceType, resourceLeaf.Position);

                if (!resourceLeaf.Value.IsNumber || resourceLeaf.Value.IsNegativeNumber)
                {
                    _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.FailedStringToIntError,
                        FilePath, resourceLeaf.Position, $"战略资源数量 '{amount}' 无法转换为非负整数"));
                }
            }
        }
    }

    private void CheckResourceIsExisting(string  resourceType, Position position)
    {
        if (!_resourcesTypeSet.Contains(resourceType))
        {
            _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.InvalidValue, FilePath, position, $"战略资源类型 '{resourceType}' 不存在"));
        }
    }

    public static void Clear()
    {
        ExistingProvinces.Clear();
        ExistingIds.Clear();
    }

    private static class Keywords
    {
        public const string LocalSupplies = "local_supplies";
    }
}
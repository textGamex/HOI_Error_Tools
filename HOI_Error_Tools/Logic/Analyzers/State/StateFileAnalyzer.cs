using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private static readonly ConcurrentBag<Province> existingProvinces = new();
    private static readonly ConcurrentDictionary<uint, ConcurrentBag<string>> repeatedProvinceFilePathMap = new();

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
                _filePath, new Position(parser.GetError()), "解析错误", ErrorType.ParseError));
            return errorList;
        }

        var result = parser.GetResult();
        var stateModel = new StateModel(result);
        if (result.HasNot(ScriptKeyWords.State))
        {
            var errorMessage = ErrorMessage.CreateSingleFileError(_filePath, $"'{ScriptKeyWords.State}' 不存在", ErrorType.MissingKeyword);
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
            ScriptKeyWords.History,
            ScriptKeyWords.Provinces
            ));
        errorList.AddRange(helper.AssertKeywordExistsInChild(ScriptKeyWords.History, ScriptKeyWords.Owner));
        errorList.AddRange(AssertProvinces(result));
        errorList.AddRange(AssertBuildings(result));
        errorList.AddRange(AssertResourcesTypeIsRegistered(result));

        return errorList;
    }

    /// <summary>
    /// 如果 Provinces Key 不存在, 返回空集合
    /// </summary>
    /// <param name="root"></param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertProvinces(Node root)
    {
        if (root.HasNot(ScriptKeyWords.Provinces))
        {
            return Enumerable.Empty<ErrorMessage>();
        }
        var errorList = new List<ErrorMessage>();
        var provincesNode = root.Child(ScriptKeyWords.Provinces).Value;
        var provincesSet = provincesNode.LeafValues.Select(p => uint.Parse(p.Key)).ToHashSet();

        var position = new Position(provincesNode.Position);
        errorList.AddRange(AssertProvincesIsRegistered(provincesSet, position));
        errorList.AddRange(AssertProvincesNotRepeat(provincesSet, position));

        return errorList;
    }

    private IEnumerable<ErrorMessage> AssertProvincesIsRegistered(IEnumerable<uint> provinces, Position position)
    {
        var errorList = new List<ErrorMessage>(16);
        foreach (var province in provinces)
        {
            if (_registeredProvince.Contains(province))
            {
                continue;
            }

            errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                _filePath, position, $"Province {province} 未在文件中注册", ErrorType.UnexpectedValue));
        }

        return errorList;
    }

    /// <summary>
    /// 检查Provinces 是否重复
    /// </summary>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertProvincesNotRepeat(HashSet<uint> provincesSet, Position position)
    {
        var errorList = new List<ErrorMessage>();

        foreach (var u in provincesSet)
        {
            if (repeatedProvinceFilePathMap.TryGetValue(u, out var filePathList))
            {
                filePathList.Add(_filePath);
                continue;
            }
            foreach (var existingProvince in existingProvinces)
            {
                if (existingProvince.IsExists(u))
                {
                    errorList.Add(new ErrorMessage(
                            GetRepeatProvinceFilePaths(u, _filePath),
                            position,
                            $"Province {u} 重复分配",
                            ErrorType.DuplicateValue));
                }
            }
        }

        existingProvinces.Add(new Province(_filePath, position.Line, provincesSet));
        return errorList;
    }

    /// <summary>
    /// 获得重复小地块所在文件路径的集合, 如果是第一次检查到重复, 创建一个新集合注册并返回
    /// </summary>
    /// <param name="province"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private static IEnumerable<string> GetRepeatProvinceFilePaths(uint province, string filePath)
    {
        if (repeatedProvinceFilePathMap.TryGetValue(province, out var filePathList))
        {
            filePathList.Add(filePath);
            return filePathList;
        }

        return RegisterToRepeatedProvinceFilePathMap(province, filePath);
    }

    private static IEnumerable<string> RegisterToRepeatedProvinceFilePathMap(uint province, string filePath)
    {
        var list = new ConcurrentBag<string> { filePath };
        if (!repeatedProvinceFilePathMap.TryAdd(province, list))
        {
            throw new ArgumentException("数据添加失败");
        }
        return list;
    }

    private IEnumerable<ErrorMessage> AssertBuildings(Node result)
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
                        _filePath, new Position(leaf.Position), $"数值 '{leaf.ValueText}' 解析失败", ErrorType.UnexpectedValue));
                    continue;
                }

                if (level > buildingInfo.MaxLevel)
                {
                    errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                        _filePath,
                        new Position(leaf.Position),
                        $"建筑物等级: {level} 超过最大值: {buildingInfo.MaxLevel}",
                        ErrorType.UnexpectedValue));
                }
            }
            else
            {
                errorMessages.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                    _filePath, new Position(leaf.Position), $"建筑物类型 '{leaf.Key}' 不存在", ErrorType.UnexpectedValue));
            }
        }

        return errorMessages;
    }

    private IEnumerable<ErrorMessage> AssertResourcesTypeIsRegistered(Node rootNode)
    {
        if (rootNode.HasNot(ScriptKeyWords.Resources))
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var errorMessages = new List<ErrorMessage>();

        foreach (var leaf in rootNode.Child(ScriptKeyWords.Resources).Value.Leaves)
        {
            if (!_resourcesTypeSet.Contains(leaf.Key))
            {
                errorMessages.Add(
                    ErrorMessage.CreateSingleFileErrorWithPosition(_filePath, new Position(leaf.Position), "资源类型不存在", ErrorType.UnexpectedValue));
            }
        }

        return errorMessages;
    }

    public static void Clear()
    {
        existingProvinces.Clear();
        repeatedProvinceFilePathMap.Clear();
    }

    private sealed class Province
    {
        public string FilePath { get; }
        public long Line { get; }
        private readonly HashSet<uint> _provinces;

        public Province(string filePath, long line, HashSet<uint> provinces)
        {
            FilePath = filePath;
            Line = line;
            _provinces = provinces;
        }

        public bool IsExists(uint province)
        {
            return _provinces.Contains(province);
        }
    }
}
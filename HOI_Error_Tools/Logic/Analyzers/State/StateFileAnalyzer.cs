using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.State;

public class StateFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;

    /// <summary>
    /// 在文件中注册的省份ID
    /// </summary>
    private readonly IReadOnlySet<uint> _registeredProvince;
    private static readonly ConcurrentBag<Province> existingProvinces = new();
    private static readonly ConcurrentDictionary<uint, List<string>> repeatedProvinceFilePathMap = new();

    public StateFileAnalyzer(string filePath, GameResources resources)
    {
        _filePath = filePath;
        _registeredProvince = resources.RegisteredProvinceSet;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var parser = new Parser(_filePath);
        var errorList = new List<ErrorMessage>();

        if (parser.IsFailure)
        {
            errorList.Add(ErrorMessage.CreateSingleFileErrorWithPosition(
                _filePath, new Position(parser.GetError()), "解析错误", ErrorType.ParseError));
            return errorList;
        }

        var result = parser.GetResult();
        if (result.HasNot(ScriptKeyWords.State))
        {
            var errorMessage = ErrorMessage.CreateSingleFileError(_filePath, $"'{ScriptKeyWords.State}' 不存在", ErrorType.MissingKeyword);
            errorList.Add(errorMessage);
            return errorList;
        }

        result = result.Child(ScriptKeyWords.State).Value;
        errorList.AddRange(AssertKeywordExistsInCurrentNode(result,
            ScriptKeyWords.Id,
            ScriptKeyWords.StateCategory,
            ScriptKeyWords.Manpower,
            ScriptKeyWords.Name,
            ScriptKeyWords.History,
            ScriptKeyWords.Provinces
            ));
        errorList.AddRange(AssertKeywordExistsInChild(result, ScriptKeyWords.History, ScriptKeyWords.Owner));
        errorList.AddRange(AssertProvinces(result));

        return errorList;
    }

    /// <summary>
    /// 检查关键字在当前节点是否存在, 如果不存在, 返回 <see cref="ErrorMessage"/>
    /// </summary>
    /// <param name="node">检测的节点</param>
    /// <param name="keys">关键字</param>
    /// <returns><c>ErrorMessage</c></returns>
    private IEnumerable<ErrorMessage> AssertKeywordExistsInCurrentNode(Node node, params string[] keys)
    {
        var errorMessageList = new List<ErrorMessage>(keys.Length);
        foreach (var key in keys)
        {
            if (node.HasNot(key))
            {
                errorMessageList.Add(ErrorMessage.CreateSingleFileError(_filePath, $"'{key}' 不存在", ErrorType.MissingKeyword));
            }
        }
        return errorMessageList;
    }

    /// <summary>
    /// 检查关键字是否在传入节点的孩子中, 如果孩子不存在, 返回空集合
    /// </summary>
    /// <param name="result"></param>
    /// <param name="childName">孩子名称</param>
    /// <param name="keys">需要检查的关键字</param>
    /// <returns></returns>
    private IEnumerable<ErrorMessage> AssertKeywordExistsInChild(Node result, string childName, params string[] keys)
    {
        if (result.HasNot(childName))
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var childNode = result.Child(childName).Value;

        return AssertKeywordExistsInCurrentNode(childNode, keys);
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
        var list = new List<string> { filePath };
        if (!repeatedProvinceFilePathMap.TryAdd(province, list))
        {
            throw new ArgumentException("数据添加失败");
        }
        return list;
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
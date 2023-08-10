using System;
using System.Collections.Generic;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;
    private readonly IReadOnlySet<string> _registeredCountriesTag;
    private readonly IReadOnlySet<string> _registeredIdeas;
    private readonly IReadOnlySet<string> _registeredIdeologies;
    private readonly AnalyzerHelper _helper;

    public CountryDefineFileAnalyzer(string filePath, GameResources resources)
    {
        _filePath = filePath;
        _helper = new AnalyzerHelper(_filePath);
        _registeredCountriesTag = resources.RegisteredCountriesTag;
        _registeredIdeologies = resources.RegisteredIdeologies;
        _registeredIdeas = resources.RegisteredIdeas;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var errorList = new List<ErrorMessage>();

        var parser = new CWToolsParser(_filePath);
        if (parser.IsFailure)
        {
            errorList.Add(ErrorMessageFactory.CreateParseErrorMessage(_filePath, parser.GetError()));
            return errorList;
        }
        var model = new CountryDefineFileModel(parser.GetResult());
        
        errorList.AddRange(AnalyzePopularities(model));
        errorList.AddRange(AnalyzeIdeas(model));
        errorList.AddRange(AnalyzePolitics(model));

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzePolitics(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        var keyMap = new Dictionary<string, Value.Types>(4)
        {
            { "ruling_party", Value.Types.String },
            { "last_election", Value.Types.Date },
            { "election_frequency", Value.Types.Integer },
            { "elections_allowed", Value.Types.Boolean }
        };
        var errorMessage = _helper.AssertExistKeyword(model.SetPoliticsList, "set_politics", ErrorLevel.Warn);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var leavesNode in model.SetPoliticsList)
        {
            errorList.AddRange(_helper.AssertKeywordsIsValid(leavesNode, keyMap.Keys.ToHashSet()));
            errorList.AddRange(_helper.AssertValueTypeIsExpected(leavesNode, keyMap));

            var rulingParty = TryGetLeafContent(leavesNode, "ruling_party");
            if (rulingParty is null)
            {
                continue;
            }
            if (!rulingParty.Value.IsString)
            {
                errorList.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                    _filePath, rulingParty, Enum.GetName(Value.Types.String) ?? string.Empty));
                continue;
            }

            if (!_registeredIdeologies.Contains(rulingParty.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, rulingParty.Position, $"意识形态 '{rulingParty.ValueText}' 未定义"));
            }

            if (!model.SetPopularitiesList.Any(node => node.Leaves.Any(leaf => leaf.Key == rulingParty.ValueText)))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, rulingParty.Position, $"执政党 '{rulingParty.ValueText}' 未在 'set_popularities' 中设置支持率"));
            }
        }
        return errorList;
    }

    private static LeafContent? TryGetLeafContent(LeavesNode node, string key)
    {
        return node.Leaves.FirstOrDefault(leafContent => leafContent.Key == key);
    }

    //private IEnumerable<ErrorMessage> AnalyzePoliticsKeywords(LeavesNode node, IEnumerable<string> keywords)
    //{
    //    foreach (var keyword in keywords)
    //    {
    //        var leaf = node.Leaves.FirstOrDefault(l => l.Key == keyword);
    //        if (leaf is null)
    //        {
    //            yield return ErrorMessageFactory.CreateSingleFileErrorWithPosition(
    //                _filePath, node.Position, $"缺少关键字 '{keyword}'");
    //        }
    //    }
    //}

    private IEnumerable<ErrorMessage> AnalyzeIdeas(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        foreach (var ideasNode in model.OwnIdeaNodes)
        {
            foreach (var idea in ideasNode.LeafValueContents)
            {
                if (!_registeredIdeas.Contains(idea.ValueText))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, idea.Position, $"Idea '{idea.ValueText}' 未定义"));
                }
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzePopularities(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();

        var errorMessage = _helper.AssertExistKeyword(model.SetPopularitiesList, "set_popularities");
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var popularities in model.SetPopularitiesList)
        {
            uint sum = 0;
            foreach (var popularity in popularities.Leaves)
            {
                var ideologiesName = popularity.Key;
                var proportionText = popularity.ValueText;
                if (!_registeredIdeologies.Contains(ideologiesName))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, popularity.Position, $"未注册的意识形态 '{ideologiesName}'"));
                }

                if (!popularity.Value.IsInt)
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, popularity.Position, $"'{proportionText}' 无法转化为整数"));
                    continue;
                }
                sum += uint.Parse(proportionText);
            }

            if (sum != 100)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, popularities.Position, "政党支持率总和不为100"));
            }
        }
        return errorList;
    }
}
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer : AnalyzerBase
{
    private string CountryTag => FileName[..3];
    private readonly IReadOnlySet<string> _registeredCountriesTag;
    private readonly IReadOnlySet<string> _registeredIdeas;
    private readonly IReadOnlySet<string> _registeredIdeologies;
    private readonly IReadOnlySet<string> _registeredTechnologies;
    private readonly IReadOnlySet<string> _registeredAutonomousState;
    //private readonly IReadOnlySet<string> _registeredEquipments;
    private readonly AnalyzerHelper _helper;

    public CountryDefineFileAnalyzer(string filePath, GameResources resources) : base(filePath)
    {
        _helper = new AnalyzerHelper(FilePath);
        _registeredCountriesTag = resources.RegisteredCountriesTag;
        _registeredIdeologies = resources.RegisteredIdeologies;
        _registeredIdeas = resources.RegisteredIdeas;
        _registeredTechnologies = resources.RegisteredTechnologiesSet;
        //_registeredEquipments = resources.RegisteredEquipmentSet;
        _registeredAutonomousState = resources.RegisteredAutonomousState;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var errorList = new List<ErrorMessage>();

        var parser = new CWToolsParser(FilePath);
        if (parser.IsFailure)
        {
            errorList.Add(ErrorMessageFactory.CreateParseErrorMessage(FilePath, parser.GetError()));
            return errorList;
        }
        var model = new CountryDefineFileModel(parser.GetResult());
        
        errorList.AddRange(AnalyzePopularities(model));
        errorList.AddRange(AnalyzeIdeas(model));
        errorList.AddRange(AnalyzePolitics(model));
        errorList.AddRange(AnalyzeCapitals(model));
        errorList.AddRange(AnalyzePuppets(model));
        errorList.AddRange(AnalyzeCountriesTagOfAddToFaction(model));
        errorList.AddRange(AnalyzeSetAutonomys(model));
        errorList.AddRange(AnalyzeSetTechnologies(model));
        errorList.AddRange(AnalyzeGiveGuaranteeCountriesTag(model));

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeGiveGuaranteeCountriesTag(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        foreach (var leafContent in model.GiveGuaranteeCountriesTag)
        {
            if (!_registeredCountriesTag.Contains(leafContent.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                                       ErrorCode.CountryTagNotExists, FilePath, 
                                       leafContent.Position, $"被 '{CountryTag}' 保障的国家 '{leafContent.ValueText}' 不存在"));
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeSetTechnologies(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        foreach (var leavesNode in model.SetTechnologies)
        {
            foreach (var leafContent in leavesNode.Leaves)
            {
                if (!_registeredTechnologies.Contains(leafContent.Key))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.TechnologyNotExists,
                        FilePath, leafContent.Position, $"科技 '{leafContent.Key}' 不存在"));
                }
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeSetAutonomys(CountryDefineFileModel model)
    {
        if (model.SetAutonomies.Count == 0)
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var errorList = new List<ErrorMessage>();
        const string targetKey = "target";
        const string autonomousStateKey = "autonomous_state";
        var keywordMap = new Dictionary<string, Value.Types>(3)
        {
            { targetKey, Value.Types.String },
            { autonomousStateKey, Value.Types.String },
            { "freedom_level", Value.Types.Float }
        };
        
        foreach (var setAutonomyNode in model.SetAutonomies)
        {
            errorList.AddRange(_helper.AssertValueTypeIsExpected(setAutonomyNode, keywordMap));
            var target = TryGetLeafContent(setAutonomyNode, targetKey);
            if (target is null)
            {
                errorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, targetKey));
                continue;
            }

            if (!_registeredCountriesTag.Contains(target.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CountryTagNotExists,
                    FilePath, target.Position, $"国家Tag '{target.ValueText}' 不存在"));
            }

            var autonomousState = TryGetLeafContent(setAutonomyNode, autonomousStateKey);
            if (autonomousState is null)
            {
                errorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, autonomousStateKey));
                continue;
            }
            if (!_registeredAutonomousState.Contains(autonomousState.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, autonomousState.Position, $"自治等级 '{autonomousState.ValueText}' 未注册"));
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzePolitics(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        var errorMessage = _helper.AssertExistKeyword(model.SetPoliticsList, "set_politics", ErrorLevel.Warn);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        const string rulingPartyKey = "ruling_party";
        var keyMap = new Dictionary<string, Value.Types>(4)
        {
            { rulingPartyKey, Value.Types.String },
            { "last_election", Value.Types.Date },
            { "election_frequency", Value.Types.Integer },
            { "elections_allowed", Value.Types.Boolean }
        };
        
        foreach (var leavesNode in model.SetPoliticsList)
        {
            errorList.AddRange(_helper.AssertKeywordsIsValid(leavesNode, keyMap.Keys.ToHashSet()));
            errorList.AddRange(_helper.AssertValueTypeIsExpected(leavesNode, keyMap));

            var rulingParty = TryGetLeafContent(leavesNode, rulingPartyKey);
            if (rulingParty is null)
            {
                continue;
            }
            if (!rulingParty.Value.IsString)
            {
                errorList.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                    FilePath, rulingParty, Enum.GetName(Value.Types.String) ?? string.Empty));
                continue;
            }

            if (!_registeredIdeologies.Contains(rulingParty.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, rulingParty.Position, $"意识形态 '{rulingParty.ValueText}' 未定义"));
            }

            if (!model.SetPopularitiesList.Any(node => node.Leaves.Any(leaf => leaf.Key == rulingParty.ValueText)))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue,
                    FilePath, rulingParty.Position, $"执政党 '{rulingParty.ValueText}' 未在 'set_popularities' 中设置支持率"));
            }
        }
        return errorList;
    }

    private static LeafContent? TryGetLeafContent(LeavesNode node, string key)
    {
        return node.Leaves.FirstOrDefault(leafContent => leafContent.Key == key);
    }

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
                        ErrorCode.InvalidValue, FilePath, idea.Position, $"Idea '{idea.ValueText}' 未定义"));
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
                        ErrorCode.InvalidValue, FilePath, popularity.Position, $"未注册的意识形态 '{ideologiesName}'"));
                }

                if (!popularity.Value.IsInt)
                {
                    errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(FilePath, popularity));
                    continue;
                }
                sum += uint.Parse(proportionText, CultureInfo.InvariantCulture);
            }

            if (sum != 100)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, popularities.Position, "政党支持率总和不为100"));
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeCapitals(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();
        const string capitalKey = "capital";
        var errorMessage = _helper.AssertExistKeyword(model.Capitals, capitalKey);
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        // TODO: 未定义的首都 State

        errorList.AddRange(_helper.AssertValueTypeIsExpected(model.Capitals, Value.Types.Integer));
        errorList.AddRange(_helper.AssertKeywordIsOnly(model.Capitals, capitalKey));
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzePuppets(CountryDefineFileModel model)
    {
        if (model.Puppets.Count == 0)
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var errorList = new List<ErrorMessage>();
        foreach (var puppet in model.Puppets)
        {
            if (!_registeredCountriesTag.Contains(puppet.ValueText))
            {
                 errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                     ErrorCode.CountryTagNotExists,
                     FilePath, puppet.Position, $"国家Tag '{puppet.ValueText}' 未定义, 却在 '{puppet.Key}' 中使用"));
            }
        }
        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzeCountriesTagOfAddToFaction(CountryDefineFileModel model)
    {
        if (model.CountriesTagOfAddToFaction.Count == 0)
        {
            return Enumerable.Empty<ErrorMessage>();
        }

        var errorList = new List<ErrorMessage>();
        foreach (var leafContent in model.CountriesTagOfAddToFaction)
        {
            if (!_registeredCountriesTag.Contains(leafContent.ValueText))
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CountryTagNotExists,
                    FilePath, leafContent.Position, $"国家 Tag '{leafContent.ValueText}' 未注册, 却在 '{leafContent.Key}' 中使用"));
            }
        }

        return errorList;
    }
}
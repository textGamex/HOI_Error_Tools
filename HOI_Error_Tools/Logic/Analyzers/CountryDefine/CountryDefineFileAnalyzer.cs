using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

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
    private readonly IReadOnlySet<string> _registeredCharacters;
    private readonly IReadOnlySet<string> _registeredOobFileNames;
    private readonly List<ErrorMessage> _errorList = new ();

    public CountryDefineFileAnalyzer(string filePath, GameResources resources) : base(filePath)
    {
        _registeredCountriesTag = resources.RegisteredCountriesTag;
        _registeredIdeologies = resources.RegisteredIdeologies;
        _registeredIdeas = resources.RegisteredIdeas;
        _registeredTechnologies = resources.RegisteredTechnologiesSet;
        //_registeredEquipments = resources.RegisteredEquipmentSet;
        _registeredAutonomousState = resources.RegisteredAutonomousState;
        _registeredCharacters = resources.RegisteredCharacters;
        _registeredOobFileNames = resources.RegisteredOobFileNames;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        var rootNode = ParseHelper.ParseFileToNode(_errorList, FilePath);
        if (rootNode is null)
        {
            return _errorList;
        }
        var model = new CountryDefineFileModel(rootNode);
        AnalyzePopularities(model);
        AnalyzeIdeas(model);
        AnalyzePolitics(model);
        AnalyzeCapitals(model);
        AnalyzePuppets(model);
        AnalyzeCountriesTagOfAddToFaction(model);
        AnalyzeSetAutonomys(model);
        AnalyzeSetTechnologies(model);
        AnalyzeGiveGuaranteeCountriesTag(model);
        AnalyzeOwnCharacters(model);
        AnalyzeOwnOobs(model);

        return _errorList;
    }

    private void AnalyzeOwnOobs(CountryDefineFileModel model)
    {
        foreach (var oobLeaf in model.OwnOobs)
        {
            if (!_registeredOobFileNames.Contains(oobLeaf.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, oobLeaf.Position, $"文件 '{FileName}' 中使用不存在的 oob 文件 '{oobLeaf.ValueText}'"));
            }
        }
    }

    private void AnalyzeOwnCharacters(CountryDefineFileModel model)
    {
        foreach (var characterLeaf in model.OwnCharacters)
        {
            if (!_registeredCharacters.Contains(characterLeaf.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CharacterNotExists, FilePath, characterLeaf.Position, $"Character '{characterLeaf.ValueText}' 不存在, 却在文件 '{FileName}' 中被使用"));
            }
        }
    }

    private void AnalyzeGiveGuaranteeCountriesTag(CountryDefineFileModel model)
    {
        foreach (var leafContent in model.GiveGuaranteeCountriesTag)
        {
            if (!_registeredCountriesTag.Contains(leafContent.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                                       ErrorCode.CountryTagNotExists, FilePath, 
                                       leafContent.Position, $"被 '{CountryTag}' 保障的国家 '{leafContent.ValueText}' 不存在"));
            }
        }
    }

    private void AnalyzeSetTechnologies(CountryDefineFileModel model)
    {
        foreach (var leavesNode in model.SetTechnologies)
        {
            foreach (var leafContent in leavesNode.Leaves)
            {
                if (!_registeredTechnologies.Contains(leafContent.Key))
                {
                    _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.TechnologyNotExists,
                        FilePath, leafContent.Position, $"科技 '{leafContent.Key}' 不存在, 却在文件 '{FileName}' 中被使用"));
                }
            }
        }
    }

    private void AnalyzeSetAutonomys(CountryDefineFileModel model)
    {
        if (model.SetAutonomies.Count == 0)
        {
            return;
        }

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
            _errorList.AddRange(Helper.AssertValueTypeIsExpected(setAutonomyNode, keywordMap));
            var target = TryGetLeafContent(setAutonomyNode, targetKey);
            if (target is null)
            {
                _errorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, targetKey));
                continue;
            }

            if (!_registeredCountriesTag.Contains(target.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CountryTagNotExists,
                    FilePath, target.Position, $"国家Tag '{target.ValueText}' 不存在"));
            }

            var autonomousState = TryGetLeafContent(setAutonomyNode, autonomousStateKey);
            if (autonomousState is null)
            {
                _errorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, autonomousStateKey));
                continue;
            }
            if (!_registeredAutonomousState.Contains(autonomousState.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, autonomousState.Position, $"自治等级 '{autonomousState.ValueText}' 未注册"));
            }
        }
    }

    private void AnalyzePolitics(CountryDefineFileModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.SetPoliticsList, "set_politics", ErrorLevel.Warn);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
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
            _errorList.AddRange(Helper.AssertKeywordsIsValid(leavesNode, keyMap.Keys.ToHashSet()));
            _errorList.AddRange(Helper.AssertValueTypeIsExpected(leavesNode, keyMap));

            var rulingParty = TryGetLeafContent(leavesNode, rulingPartyKey);
            if (rulingParty is null)
            {
                continue;
            }
            if (!rulingParty.Value.IsString)
            {
                _errorList.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                    FilePath, rulingParty, Enum.GetName(Value.Types.String) ?? string.Empty));
                continue;
            }

            if (!_registeredIdeologies.Contains(rulingParty.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, rulingParty.Position, $"意识形态 '{rulingParty.ValueText}' 未定义"));
            }

            if (!model.SetPopularitiesList.Any(node => node.Leaves.Any(leaf => leaf.Key == rulingParty.ValueText)))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue,
                    FilePath, rulingParty.Position, $"执政党 '{rulingParty.ValueText}' 未在 'set_popularities' 中设置支持率"));
            }
        }
    }

    private static LeafContent? TryGetLeafContent(LeavesNode node, string key)
    {
        return node.Leaves.FirstOrDefault(leafContent => leafContent.Key == key);
    }

    private void AnalyzeIdeas(CountryDefineFileModel model)
    {
        foreach (var ideasNode in model.OwnIdeaNodes)
        {
            foreach (var idea in ideasNode.LeafValueContents)
            {
                if (!_registeredIdeas.Contains(idea.ValueText))
                {
                    _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.InvalidValue, FilePath, idea.Position, $"Idea '{idea.ValueText}' 未定义"));
                }
            }
        }
    }

    private void AnalyzePopularities(CountryDefineFileModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.SetPopularitiesList, "set_popularities");
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
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
                    _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.InvalidValue, FilePath, popularity.Position, $"未注册的意识形态 '{ideologiesName}'"));
                }

                if (!popularity.Value.IsInt)
                {
                    _errorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(FilePath, popularity));
                    continue;
                }
                sum += uint.Parse(proportionText, CultureInfo.InvariantCulture);
            }

            if (sum != 100)
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, popularities.Position, "政党支持率总和不为100"));
            }
        }
    }

    private void AnalyzeCapitals(CountryDefineFileModel model)
    {
        const string capitalKey = "capital";
        var errorMessage = Helper.AssertExistKeyword(model.Capitals, capitalKey);
        if (errorMessage is not null)
        {
            _errorList.Add(errorMessage);
            return;
        }

        // TODO: 未定义的首都 State

        _errorList.AddRange(Helper.AssertValueTypeIsExpected(model.Capitals, Value.Types.Integer));
        _errorList.AddRange(Helper.AssertKeywordIsOnly(model.Capitals, capitalKey));
        return;
    }

    private void AnalyzePuppets(CountryDefineFileModel model)
    {
        if (model.Puppets.Count == 0)
        {
            return;
        }

        foreach (var puppet in model.Puppets)
        {
            if (!_registeredCountriesTag.Contains(puppet.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                     ErrorCode.CountryTagNotExists,
                     FilePath, puppet.Position, $"国家Tag '{puppet.ValueText}' 未定义, 却在 '{puppet.Key}' 中使用"));
            }
        }
    }

    private void AnalyzeCountriesTagOfAddToFaction(CountryDefineFileModel model)
    {
        if (model.CountriesTagOfAddToFaction.Count == 0)
        {
            return;
        }

        foreach (var leafContent in model.CountriesTagOfAddToFaction)
        {
            if (!_registeredCountriesTag.Contains(leafContent.ValueText))
            {
                _errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CountryTagNotExists,
                    FilePath, leafContent.Position, $"国家 Tag '{leafContent.ValueText}' 未注册, 却在 '{leafContent.Key}' 中使用"));
            }
        }
    }
}
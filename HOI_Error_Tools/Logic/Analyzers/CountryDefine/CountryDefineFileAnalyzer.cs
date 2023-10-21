using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.Game;
using Microsoft.Extensions.DependencyInjection;
using NLog;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public sealed partial class CountryDefineFileAnalyzer : AnalyzerBase
{
    private readonly GameResources _resources;
    private static readonly ILogger Log = App.Current.Services.GetRequiredService<ILogger>();

    public CountryDefineFileAnalyzer(string filePath, GameResources resources) : base(filePath)
    {
        _resources = resources;
    }

    public override IEnumerable<ErrorMessage> GetErrorMessages()
    {
        Node? rootNode = null;
        try
        {
            rootNode = ParseHelper.ParseFileToNode(ErrorList, FilePath);
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
            return ErrorList;
        }
        
        var model = new CountryDefineFileModel(rootNode);
        AnalyzePopularities(model);
        AnalyzeIdeas(model);
        AnalyzePolitics(model);
        AnalyzeCapitals(model);
        AnalyzeUsedCountryTags(model);
        AnalyzeSetAutonomys(model);
        AnalyzeSetTechnologies(model);
        AnalyzeOwnCharacters(model);
        AnalyzeOwnOobs(model);
        AnalyzeUsedVariable(model.UsedVariable);

        return ErrorList;
    }

    private void AnalyzeUsedVariable(IReadOnlyCollection<LeavesNode> usedVariable)
    {
        // examples: 
        // set_variable = { var = wehrmacht_anger value = 0 } or
        // set_variable = { SWI_neutral_opinion = 10 }
        var leavesList = new List<LeafContent>(usedVariable.Count);
        foreach (var leavesNode in usedVariable)
        {
            var leaves = leavesNode.Leaves.ToList();
            if (leaves.Count == 2)
            {
                if (!leaves[0].Key.Equals("var", StringComparison.OrdinalIgnoreCase) ||
                    !leaves[1].Key.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(ErrorCode.FormatIsInvalid,
                        FilePath, leavesNode.Position, "set_variable 关键字使用错误"));
                    continue;
                }
                    
                var variableName = leaves[0].ValueText;
                var variableValue = leaves[1].Value;
                leavesList.Add(new LeafContent(variableName, variableValue, leavesNode.Position));
            }
            else if (leaves.Count == 1)
            {
                leavesList.Add(leaves[0]);
            }
            else
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(ErrorCode.FormatIsInvalid,
                    FilePath, leavesNode.Position, "set_variable 格式错误"));
            }
        }
        ErrorList.AddRange(Helper.AssertValueIsOnly(leavesList, variableName => $"重复设置的变量 '{variableName}'",
            leaf => leaf.Key));
    }

    private void AnalyzeOwnOobs(CountryDefineFileModel model)
    {
        foreach (var oobLeaf in model.OwnOobs)
        {
            var oobFileName = oobLeaf.ValueText;
            if (!_resources.RegisteredOobFileNames.Contains(oobFileName))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, oobLeaf.Position, $"文件 '{FileName}' 中使用不存在的 oob 文件 '{oobFileName}'"));
            }
        }
    }

    private void AnalyzeOwnCharacters(CountryDefineFileModel model)
    {
        foreach (var characterLeaf in model.OwnCharacters)
        {
            var characterName = characterLeaf.ValueText;
            if (!_resources.RegisteredCharacters.Contains(characterName))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CharacterNotExists, FilePath, characterLeaf.Position, 
                    $"Character '{characterName}' 不存在, 却在文件 '{FileName}' 中被使用"));
            }
        }
    }
    
    private void AnalyzeSetTechnologies(CountryDefineFileModel model)
    {
        foreach (var leavesNode in model.SetTechnologies)
        {
            foreach (var leafContent in leavesNode.Leaves)
            {
                if (!_resources.RegisteredTechnologiesSet.Contains(leafContent.Key))
                {
                    ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
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
            ErrorList.AddRange(Helper.AssertValueTypeIsExpected(setAutonomyNode, keywordMap));
            var targetCountryTag = TryGetLeafContent(setAutonomyNode, targetKey);
            if (targetCountryTag is null)
            {
                ErrorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, targetKey));
                continue;
            }

            if (!_resources.RegisteredCountriesTag.Contains(targetCountryTag.ValueText))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.CountryTagNotExists,
                    FilePath, targetCountryTag.Position, $"国家Tag '{targetCountryTag.ValueText}' 不存在"));
            }

            var autonomousState = TryGetLeafContent(setAutonomyNode, autonomousStateKey);
            if (autonomousState is null)
            {
                ErrorList.Add(ErrorMessageFactory.CreateKeywordIsMissingErrorMessage(FilePath, setAutonomyNode, autonomousStateKey));
                continue;
            }
            if (!_resources.RegisteredAutonomousState.Contains(autonomousState.ValueText))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, autonomousState.Position, $"自治等级 '{autonomousState.ValueText}' 未注册"));
            }
        }
    }

    private void AnalyzePolitics(CountryDefineFileModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.SetPoliticsList, "set_politics", ErrorLevel.Warn);
        if (errorMessage is not null)
        {
            ErrorList.Add(errorMessage);
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
            ErrorList.AddRange(Helper.AssertKeywordsIsValid(leavesNode, keyMap.Keys.ToHashSet()));
            ErrorList.AddRange(Helper.AssertValueTypeIsExpected(leavesNode, keyMap));

            var rulingParty = TryGetLeafContent(leavesNode, rulingPartyKey);
            // TODO: 减少重复代码 (检查关键字是否存在)
            if (rulingParty is null)
            {
                continue;
            }
            if (!rulingParty.Value.IsString)
            {
                ErrorList.Add(ErrorMessageFactory.CreateInvalidValueErrorMessage(
                    FilePath, rulingParty, Enum.GetName(Value.Types.String) ?? string.Empty));
                continue;
            }

            if (!_resources.RegisteredIdeologies.Contains(rulingParty.ValueText))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, rulingParty.Position, $"意识形态 '{rulingParty.ValueText}' 未定义"));
            }

            if (!model.SetPopularitiesList.Any(node => node.Leaves.Any(leaf => leaf.Key == rulingParty.ValueText)))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
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
                if (!_resources.RegisteredIdeas.Contains(idea.ValueText))
                {
                    AddErrorMessageToList(idea.ValueText, idea.Position);
                }
            }
        }

        foreach (var ideaLeaf in model.OwnIdeaLeaves)
        {
            if (!_resources.RegisteredIdeas.Contains(ideaLeaf.ValueText))
            {
                AddErrorMessageToList(ideaLeaf.ValueText, ideaLeaf.Position);
            }
        }

        return;
        void AddErrorMessageToList(string ideaName, Position position) => 
            ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                ErrorCode.InvalidValue, FilePath, position, $"Idea '{ideaName}' 未定义"));
    }

    private void AnalyzePopularities(CountryDefineFileModel model)
    {
        var errorMessage = Helper.AssertExistKeyword(model.SetPopularitiesList, "set_popularities");
        if (errorMessage is not null)
        {
            ErrorList.Add(errorMessage);
            return;
        }

        foreach (var popularities in model.SetPopularitiesList)
        {
            uint sum = 0;
            foreach (var popularity in popularities.Leaves)
            {
                var ideologiesName = popularity.Key;
                var proportionText = popularity.ValueText;
                if (!_resources.RegisteredIdeologies.Contains(ideologiesName))
                {
                    ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        ErrorCode.InvalidValue, FilePath, popularity.Position, $"未注册的意识形态 '{ideologiesName}'"));
                }

                if (!popularity.Value.IsInt)
                {
                    ErrorList.Add(ErrorMessageFactory.CreateFailedStringToIntErrorMessage(FilePath, popularity));
                    continue;
                }
                sum += uint.Parse(proportionText, CultureInfo.InvariantCulture);
            }

            if (sum != 100)
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    ErrorCode.InvalidValue, FilePath, popularities.Position, "政党支持率总和不为 100"));
            }
        }
    }

    private void AnalyzeCapitals(CountryDefineFileModel model)
    {
        const string capitalKey = "capital";
        var errorMessage = Helper.AssertExistKeyword(model.Capitals, capitalKey);
        if (errorMessage is not null)
        {
            ErrorList.Add(errorMessage);
            return;
        }

        // TODO: 未定义的首都 State

        ErrorList.AddRange(Helper.AssertValueTypeIsExpected(model.Capitals, Value.Types.Integer));
        ErrorList.AddRange(Helper.AssertKeywordIsOnly(model.Capitals));
    }

    private void AnalyzeUsedCountryTags(CountryDefineFileModel model)
    {
        if (model.UsedCountryTags.Count == 0)
        {
            return;
        }

        foreach (var leafContent in model.UsedCountryTags)
        {
            if (!_resources.RegisteredCountriesTag.Contains(leafContent.ValueText))
            {
                ErrorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                     ErrorCode.CountryTagNotExists,
                     FilePath, leafContent.Position, $"国家Tag '{leafContent.ValueText}' 未定义, 却在 '{leafContent.Key}' 中使用"));
            }
        }
    }
}
using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer : AnalyzerBase
{
    private readonly string _filePath;
    private readonly IReadOnlySet<string> _registeredCountriesTag;
    private readonly IReadOnlySet<string> _registeredIdeologies;
    private readonly AnalyzerHelper _helper;

    public CountryDefineFileAnalyzer(string filePath, GameResources resources)
    {
        _filePath = filePath;
        _helper = new AnalyzerHelper(_filePath);
        _registeredCountriesTag = resources.RegisteredCountriesTag;
        _registeredIdeologies = resources.RegisteredIdeologies;
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

        return errorList;
    }

    private IEnumerable<ErrorMessage> AnalyzePopularities(CountryDefineFileModel model)
    {
        var errorList = new List<ErrorMessage>();

        var errorMessage = _helper.AssertExistKeyword(model.SetPopularities, "set_popularities");
        if (errorMessage is not null)
        {
            errorList.Add(errorMessage);
            return errorList;
        }

        foreach (var popularity in model.SetPopularities)
        {
            uint sum = 0;
            foreach (var (ideologiesName, proportionText, position) in popularity.Popularity)
            {
                if (!_registeredIdeologies.Contains(ideologiesName))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, position, $"未注册的意识形态 '{ideologiesName}'"));
                }

                if (!uint.TryParse(proportionText, out var proportion))
                {
                    errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                        _filePath, position, $"'{proportionText}' 无法转化为整数"));
                }
                sum += proportion;
            }

            if (sum != 100)
            {
                errorList.Add(ErrorMessageFactory.CreateSingleFileErrorWithPosition(
                    _filePath, popularity.Position, "总和不为100"));
            }
        }

        errorList.AddRange(_helper.AssertKeywordIsOnly(model.SetPopularities, "set_popularities"));
        return errorList;
    }
}
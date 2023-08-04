using System.Collections.Generic;
using System.Linq;
using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools.Logic.Analyzers.CountryDefine;

public partial class CountryDefineFileAnalyzer
{
    private sealed class CountryDefineFileModel
    {
        public IReadOnlyList<(IEnumerable<(string IdeologiesName, string Proportion, Position Position)> Popularity, Position Position)> 
            SetPopularities { get; }
        public CountryDefineFileModel(Node rootNode)
        {
            SetPopularities = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "set_popularities").ToList();
        }
    }
}
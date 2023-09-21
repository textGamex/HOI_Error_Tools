using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Util;
using System.IO;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    protected string FilePath { get; }
    protected string FileName { get; }
    protected AnalyzerHelper Helper { get; }
    protected List<ErrorMessage> ErrorList { get; }

    protected AnalyzerBase(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
        Helper = new AnalyzerHelper(FilePath, FileName);
        ErrorList = new List<ErrorMessage>(5);
    }

    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
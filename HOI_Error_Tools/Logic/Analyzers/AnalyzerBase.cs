using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Util;
using static System.IO.Path;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    protected string FilePath { get; }
    protected string FileName { get; }
    protected AnalyzerHelper Helper { get; }

    protected AnalyzerBase(string filePath)
    {
        FilePath = filePath;
        FileName = GetFileName(filePath);
        Helper = new AnalyzerHelper(FilePath, FileName);
    }

    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
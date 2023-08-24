using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    protected string FilePath { get; }
    protected string FileName { get; }

    protected AnalyzerBase(string filePath)
    {
        FilePath = filePath;
        FileName = System.IO.Path.GetFileName(filePath);
    }

    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
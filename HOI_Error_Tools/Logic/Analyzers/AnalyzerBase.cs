using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    protected string FilePath { get; }
    protected string FileName => System.IO.Path.GetFileName(FilePath);

    protected AnalyzerBase(string filePath)
    {
        FilePath = filePath;
    }
    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
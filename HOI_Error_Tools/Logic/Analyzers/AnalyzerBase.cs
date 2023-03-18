using System.Collections.Generic;
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
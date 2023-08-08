using HOI_Error_Tools.Logic.Analyzers.Error;
using System.Collections.Generic;

namespace HOI_Error_Tools.Logic.Analyzers;

public abstract class AnalyzerBase
{
    public abstract IEnumerable<ErrorMessage> GetErrorMessages();
}
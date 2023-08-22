using System;

namespace HOI_Error_Tools.Logic.Messages;

public record AnalysisCompleteMessage(int FileSum, TimeSpan ElapsedTime);
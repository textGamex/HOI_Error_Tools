using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public sealed class ParameterInfo
{
    public string FilePath { get; }
    public Position Position { get; }

    public ParameterInfo(string filePath, Position position)
    {
        FilePath = filePath;
        Position = position;
    }
}
using HOI_Error_Tools.Logic.Analyzers.Error;

namespace HOI_Error_Tools.Logic.Analyzers.Common;

public sealed class ParameterFileInfo
{
    public string FilePath { get; }
    public Position Position { get; }

    public ParameterFileInfo(string filePath, Position position)
    {
        FilePath = filePath;
        Position = position;
    }

    public override string ToString()
    {
        return $"{nameof(FilePath)}={FilePath}, {nameof(Position)}={Position}";
    }
}
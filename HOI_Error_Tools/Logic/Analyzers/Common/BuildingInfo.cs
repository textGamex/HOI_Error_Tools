namespace HOI_Error_Tools.Logic.Analyzers.Common;

public class BuildingInfo
{
    public string Name { get; }
    public ushort MaxLevel { get; }

    public BuildingInfo(string name, ushort maxLevel)
    {
        Name = name;
        MaxLevel = maxLevel;
    }
}
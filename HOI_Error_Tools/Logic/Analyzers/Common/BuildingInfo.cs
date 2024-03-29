﻿namespace HOI_Error_Tools.Logic.Analyzers.Common;

public sealed class BuildingInfo
{
    public string Name { get; }
    public ushort MaxLevel { get; }

    public BuildingInfo(string name, ushort maxLevel)
    {
        Name = name;
        MaxLevel = maxLevel;
    }

    public override string ToString()
    {
        return $"{nameof(Name)}={Name}, {nameof(MaxLevel)}={MaxLevel}";
    }
}
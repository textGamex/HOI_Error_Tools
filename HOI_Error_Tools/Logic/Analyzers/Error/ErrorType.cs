using System.ComponentModel;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorType : byte
{
    [Description("未知")]
    Unknown,

    [Description("游戏本体错误")]
    Game,

    [Description("Mod错误")]
    Modification,

    [Description("混合错误")]
    Mixing
}
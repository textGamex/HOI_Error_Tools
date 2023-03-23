using System.ComponentModel;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorType : ushort
{
    [Description("未知")]
    None,
    [Description("解析错误")]
    ParseError,
    [Description("缺少关键字")]
    MissingKeyword,

    /// <summary>
    /// 一个不允许重复的值在多个文件出现
    /// </summary>
    [Description("重复值")]
    DuplicateValue,
    [Description("非法值")]
    UnexpectedValue
}
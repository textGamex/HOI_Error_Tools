namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorType : ushort
{
    None,
    ParseError,
    MissingKeyword,
    /// <summary>
    /// 一个不允许重复的值在多个文件出现
    /// </summary>
    DuplicateValue,
    UnexpectedValue
}
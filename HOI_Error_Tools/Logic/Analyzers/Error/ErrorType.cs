namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorType : ushort
{
    None,
    ParseError,
    MissingKeyword,
    DuplicateValue,
    UnexpectedValue
}
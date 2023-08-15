namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorCode : uint
{
    ParseError = 1000,
    FailedStringToIntError,
    InvalidValue,
    EmptyFileError,
    CountryTagNotExists,
    KeywordIsMissing,
    KeywordIsRepeated,
    /// <summary>
    /// 应该唯一的值重复出现.
    /// </summary>
    UniqueValueIsRepeated,
    VictoryPointsFormatIsInvalid,
    ProvinceNotExistsInStateFile,
    ProvinceNotExistsInDefinitionCsvFile,
    StateCategoryNotExists,
    EmptyProvincesNode,
    ValueIsOutOfRange,
    /// <summary>
    /// 重复的注册.
    /// </summary>
    DuplicateRegistration,
    ResourcesNodeNotOnly,
    TechnologyNotExists,
}
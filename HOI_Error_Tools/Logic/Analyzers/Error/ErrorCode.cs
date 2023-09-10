using System.ComponentModel;

namespace HOI_Error_Tools.Logic.Analyzers.Error;

public enum ErrorCode : uint
{
    [Description("解析错误")]
    ParseError = 1000,

    [Description("不是数字")]
    FailedStringToIntError,

    [Description("无效值")]
    InvalidValue,

    [Description("文件中没有内容")]
    EmptyFileError,

    [Description("国家 Tag 未注册")]
    CountryTagNotExists,

    [Description("缺少关键字")]
    KeywordIsMissing,

    [Description("不应重复的关键字重复")]
    KeywordIsRepeated,

    /// <summary>
    /// 应该唯一的值重复出现.
    /// </summary>
    [Description("应该唯一的值重复出现")]
    UniqueValueIsRepeated,

    [Description("VictoryPoints 格式错误")]
    VictoryPointsFormatIsInvalid,

    [Description("Province 不属于这个 State, 却在这个 State 被使用")]
    ProvinceNotExistsInStateFile,

    [Description("Province 未在 Definition.cvs 注册")]
    ProvinceNotExistsInDefinitionCsvFile,

    [Description("StateCategory 未注册")]
    StateCategoryNotExists,

    [Description("空的 Provinces 块")]
    EmptyProvincesNode,

    [Description("超出范围的值")]
    ValueIsOutOfRange,

    /// <summary>
    /// 重复的注册.
    /// </summary>
    [Description("重复的注册")]
    DuplicateRegistration,

    [Description("重复声明的资源块")]
    ResourcesNodeNotOnly,

    [Description("科技不存在却被使用")]
    TechnologyNotExists,

    [Description("人物不存在")]
    CharacterNotExists,
    
    [Description("国家 Tag 格式错误")]
    CountryTagFormatIsInvalid,
}
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools_UnitTest.Logic.Analyzers.Util;

[TestFixture]
public class AnalyzerHelperTests
{
    private const string FilePath = "test.txt";
    private readonly AnalyzerHelper _analyzerHelper = new (FilePath);

    [Test]
    public void AssertExistKeyword()
    {
        var result1 = _analyzerHelper.AssertExistKeyword(Array.Empty<int>(), "test", ErrorLevel.Tip);
        var result2 = _analyzerHelper.AssertExistKeyword(new []{ 1 }, "test");

        Multiple(() =>
        {
            That(result2, Is.Null);
            That(result1, Is.Not.Null);
            That(result1!.FileInfo, Has.One.Items);
            That(result1.FileInfo.First().FilePath, Is.EqualTo(FilePath));
            That(result1.Code, Is.EqualTo(ErrorCode.KeywordIsMissing));
            That(result1.Level, Is.EqualTo(ErrorLevel.Tip));
        });

    }
}
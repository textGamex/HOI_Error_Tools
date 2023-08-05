using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools_UnitTest.Logic.Analyzers.Util;

[TestFixture]
[TestOf(typeof(ParseHelper))]
public class ParseHelperTest
{
    [Test]
    public void GetAllLeafKeyAndValueInAllNode()
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, "TestText", "ParseHelperTestText.txt");
        var parser = new HOI_Error_Tools.Logic.HOIParser.CWToolsParser(filePath);
        var rootNode = parser.GetResult();

        var result = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "bbb");

    }
    
}
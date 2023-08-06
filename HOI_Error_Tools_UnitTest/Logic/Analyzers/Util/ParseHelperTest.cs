using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;

namespace HOI_Error_Tools_UnitTest.Logic.Analyzers.Util;

[TestFixture]
[TestOf(typeof(ParseHelper))]
public class ParseHelperTest
{
    [Test]
    public void GetAllLeafKeyAndValueInAllNode()
    {
        var filePath = Path.Combine(Environment.CurrentDirectory, "Data", "TestText", "ParseHelperTestText.txt");
        var parser = new HOI_Error_Tools.Logic.HOIParser.CWToolsParser(filePath);
        var rootNode = parser.GetResult();

        var result = ParseHelper.GetAllLeafKeyAndValueInAllNode(rootNode, "bbb");
        var enumerable = new List<(IEnumerable<LeafContent>, Position)>
        {
            (new LeafContent[] {new ("ccc", "2", new Position(12)), new ("ddd", "2", new Position(13))}, new Position(11)),
            (new LeafContent[] {new ("ccc", "1", new Position(6)), new ("ddd", "1", new Position(7))}, new Position(line: 5))
        };
        Multiple(() =>
        {
            That(result, Is.EqualTo(enumerable));
        });
    }
    
}
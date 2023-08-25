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
        var filePath = Path.Combine(PathManager.TestFolderPath, "ParseHelperTestText.txt");
        var parser = new HOI_Error_Tools.Logic.HOIParser.CWToolsParser(filePath);
        var rootNode = parser.GetResult();
        var leavesNode1 = new LeavesNode("bbb",
            new LeafContent[] { new("ccc", "2", new Position(12)), new("ddd", "2", new Position(13)) },
            new Position(11));
        var leavesNode2 = new LeavesNode("bbb",
            new LeafContent[] { new("ccc", "1", new Position(6)), new("ddd", "1", new Position(7)) },
            new Position(5));
        var leavesNode3 = new LeavesNode("bbb", 
            new LeafContent[] { new("ccc", "3", new Position(18)), new("ddd", "3", new Position(19)) }, 
            new Position(17));

        var result = ParseHelper.GetAllLeafContentInRootNode(rootNode, "bbb").ToArray();

        Multiple(() =>
        {
            That(result, Has.Length.EqualTo(2));
            That(result[0].Key, Is.EqualTo(leavesNode1.Key));
            That(result[0].Leaves, Is.EquivalentTo(leavesNode1.Leaves));
            That(result[0].Position, Is.EqualTo(leavesNode1.Position));
            That(result[1].Key, Is.EqualTo(leavesNode2.Key));
            That(result[1].Leaves, Is.EquivalentTo(leavesNode2.Leaves));
            That(result[1].Position, Is.EqualTo(leavesNode2.Position));
        });
    }

}
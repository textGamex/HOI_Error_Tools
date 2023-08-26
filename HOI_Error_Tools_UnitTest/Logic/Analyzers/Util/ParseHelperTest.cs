using CWTools.Process;
using HOI_Error_Tools.Logic.Analyzers.Common;
using HOI_Error_Tools.Logic.Analyzers.Error;
using HOI_Error_Tools.Logic.Analyzers.Util;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools_UnitTest.Logic.Analyzers.Util;

[TestFixture]
[TestOf(typeof(ParseHelper))]
public class ParseHelperTest
{
    private readonly Node _rootNode = new CWToolsParser(Path.Combine(PathManager.TestFolderPath, "ParseHelperTestText.txt")).GetResult();

    [Test]
    public void GetAllLeafContentInRootNodeTest()
    {
        var leavesNode1 = new LeavesNode("bbb",
            new LeafContent[] { new("ccc", "1", new Position(6)), new("ddd", "1", new Position(7)) },
            new Position(5));
        var leavesNode2 = new LeavesNode("bbb",
            new LeafContent[] { new("ccc", "2", new Position(12)), new("ddd", "2", new Position(13)) },
            new Position(11));
        var leavesNode3 = new LeavesNode("bbb", 
            new LeafContent[] { new("dateBbbNodeLeaf", "2", new Position(21))}, 
            new Position(20));
        var leavesNode4 = new LeavesNode("bbb",
            new LeafContent[] { new("dateIfBbbNodeLeaf", "8", new Position(26)) },
            new Position(25));
        var leavesNode5 = new LeavesNode("bbb",
            new LeafContent[] { new("rootLeaf", "0", new Position(37)) },
            new Position(36));

        var result = ParseHelper.GetAllLeafContentInRootNode(_rootNode, "bbb").ToArray();

        That(result, Has.Length.EqualTo(5));
        Multiple(() =>
        {
            That(result[0].Key, Is.EqualTo(leavesNode1.Key));
            That(result[0].Leaves, Is.EquivalentTo(leavesNode1.Leaves));
            That(result[0].Position, Is.EqualTo(leavesNode1.Position));

            That(result[1].Key, Is.EqualTo(leavesNode2.Key));
            That(result[1].Leaves, Is.EquivalentTo(leavesNode2.Leaves));
            That(result[1].Position, Is.EqualTo(leavesNode2.Position));

            That(result[2].Key, Is.EqualTo(leavesNode3.Key));
            That(result[2].Leaves, Is.EquivalentTo(leavesNode3.Leaves));
            That(result[2].Position, Is.EqualTo(leavesNode3.Position));

            That(result[3].Key, Is.EqualTo(leavesNode4.Key));
            That(result[3].Leaves, Is.EquivalentTo(leavesNode4.Leaves));
            That(result[3].Position, Is.EqualTo(leavesNode4.Position));

            That(result[4].Key, Is.EqualTo(leavesNode5.Key));
            That(result[4].Leaves, Is.EquivalentTo(leavesNode5.Leaves));
            That(result[4].Position, Is.EqualTo(leavesNode5.Position));
        });
    }

    [Test]
    public void GetAllLeafContentInCurrentNodeTest()
    {
        var result = ParseHelper.GetAllLeafContentInCurrentNode(_rootNode).ToArray();

        That(result, Has.Length.EqualTo(3));
        Multiple(() =>
        {
            That(result[0].Key, Is.EqualTo("aaa"));
            That(result[0].ValueText, Is.EqualTo("1"));
            That(result[0].Position, Is.EqualTo(new Position(1)));

            That(result[1].Key, Is.EqualTo("rootLeaf"));
            That(result[1].ValueText, Is.EqualTo("0"));
            That(result[1].Position, Is.EqualTo(new Position(40)));

            That(result[2].Key, Is.EqualTo("eee"));
            That(result[2].ValueText, Is.EqualTo("7"));
            That(result[2].Position, Is.EqualTo(new Position(41)));
        });
    }

    [Test]
    public void GetLeafContentsTest()
    {
        var result = ParseHelper.GetLeafContents(_rootNode, "aaa").ToArray();

        That(result, Has.Length.EqualTo(1));
        Multiple(() =>
        {
            That(result[0].Key, Is.EqualTo("aaa"));
            That(result[0].ValueText, Is.EqualTo("1"));
            That(result[0].Position, Is.EqualTo(new Position(1)));
        });
    }

    [Test]
    public void GetLeafContentTest()
    {
        var result = ParseHelper.GetLeafContentsInAllChildren(_rootNode, "eee").ToArray();

        That(result, Has.Length.EqualTo(3));
        Multiple(() =>
        {
            That(result[0].Key, Is.EqualTo("eee"));
            That(result[0].ValueText, Is.EqualTo("5"));
            That(result[0].Position, Is.EqualTo(new Position(10)));

            That(result[1].Key, Is.EqualTo("eee"));
            That(result[1].ValueText, Is.EqualTo("6"));
            That(result[1].Position, Is.EqualTo(new Position(33)));

            That(result[2].Key, Is.EqualTo("eee"));
            That(result[2].ValueText, Is.EqualTo("7"));
            That(result[2].Position, Is.EqualTo(new Position(41)));
        });
    }
}
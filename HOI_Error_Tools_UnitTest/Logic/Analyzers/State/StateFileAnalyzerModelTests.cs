using HOI_Error_Tools.Logic.Analyzers;
using HOI_Error_Tools.Logic.Analyzers.State;
using HOI_Error_Tools.Logic.HOIParser;

namespace HOI_Error_Tools_UnitTest.Logic.Analyzers.State;

[TestFixture]
[TestOf(typeof(StateFileAnalyzer))]
public class StateFileAnalyzerModelTests
{
    private readonly StateFileAnalyzer.StateModel _stateModel = 
        new(new CWToolsParser(Path.Combine(PathManager.GameRootPath, ScriptKeyWords.History, "states", "test.txt")).GetResult());

    [Test]
    public void StateModelCountTest()
    {
        Multiple(() =>
        {
            That(_stateModel.IsEmptyFile, Is.False);
            That(_stateModel.Ids, Has.Count.EqualTo(1));
            That(_stateModel.Names, Has.Count.EqualTo(1));
            That(_stateModel.Owners, Has.Count.EqualTo(1));
            That(_stateModel.HasCoreTags, Has.Count.EqualTo(2));
            That(_stateModel.Manpowers, Has.Count.EqualTo(1));
            That(_stateModel.StateCategories, Has.Count.EqualTo(1));
            That(_stateModel.VictoryPointNodes, Has.Count.EqualTo(1));
            That(_stateModel.ProvinceNodes, Has.Count.EqualTo(1));
            That(_stateModel.Buildings, Has.Count.EqualTo(3));
            That(_stateModel.BuildingsByProvince, Has.Count.EqualTo(1));
            That(_stateModel.ResourceNodes, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void StateModelManpowerTest()
    {
        var manpower = _stateModel.Manpowers[0];

        Multiple(() =>
        {
            That(manpower, Is.Not.Null);
            That(manpower.Key, Is.EqualTo("manpower"));
            That(manpower.ValueText, Is.EqualTo("123"));
            That(manpower.Position.Line, Is.EqualTo(4));
        });
    }

    [Test]
    public void StateModelIdTest()
    {
        var id = _stateModel.Ids[0];

        Multiple(() =>
        {
            That(id, Is.Not.Null);
            That(id.Key, Is.EqualTo("id"));
            That(id.ValueText, Is.EqualTo("1"));
            That(id.Position.Line, Is.EqualTo(2));
        });
    }

    [Test]
    public void StateModelNameTest()
    {
        var name = _stateModel.Names[0];

        Multiple(() =>
        {
            That(name, Is.Not.Null);
            That(name.Key, Is.EqualTo("name"));
            That(name.ValueText, Is.EqualTo("STATE_1"));
            That(name.Position.Line, Is.EqualTo(3));
        });
    }

    [Test]
    public void StateModelOwnerTest()
    {
        var owner = _stateModel.Owners[0];

        Multiple(() =>
        {
            That(owner, Is.Not.Null);
            That(owner.Key, Is.EqualTo("owner"));
            That(owner.ValueText, Is.EqualTo("TES"));
            That(owner.Position.Line, Is.EqualTo(13));
        });
    }

    [Test]
    public void StateModelHasCoreTest()
    {
        var hasCore1 = _stateModel.HasCoreTags[0];
        var hasCore2 = _stateModel.HasCoreTags[1];

        Multiple(() =>
        {
            That(hasCore1, Is.Not.Null);
            That(hasCore1.Key, Is.EqualTo("add_core_of"));
            That(hasCore1.ValueText, Is.EqualTo("TES"));
            That(hasCore1.Position.Line, Is.EqualTo(23));

            That(hasCore1, Is.Not.Null);
            That(hasCore2.Key, Is.EqualTo("add_core_of"));
            That(hasCore2.ValueText, Is.EqualTo("AAA"));
            That(hasCore2.Position.Line, Is.EqualTo(24));
        });
    }

    [Test]
    public void StateModelStateCategoryTest()
    {
        var stateCategory = _stateModel.StateCategories[0];

        Multiple(() =>
        {
            That(stateCategory, Is.Not.Null);
            That(stateCategory.Key, Is.EqualTo("state_category"));
            That(stateCategory.ValueText, Is.EqualTo("town"));
            That(stateCategory.Position.Line, Is.EqualTo(6));
        });
    }

    [Test]
    public void StateModelVictoryPointTest()
    {
        var victoryPoint = _stateModel.VictoryPointNodes[0];
        var leafValueList = victoryPoint.LeafValueContents.ToList();

        Multiple(() =>
        {
            That(victoryPoint, Is.Not.Null);
            That(victoryPoint.Key, Is.EqualTo("victory_points"));
            That(victoryPoint.Position.Line, Is.EqualTo(14));
        });
        That(leafValueList, Has.Count.EqualTo(2));
        That(leafValueList[0].ValueText, Is.EqualTo("3838"));
        That(leafValueList[0].Position.Line, Is.EqualTo(14));
        That(leafValueList[1].ValueText, Is.EqualTo("1"));
        That(leafValueList[1].Position.Line, Is.EqualTo(14));
    }

    [Test]
    public void StateModelProvinceTest()
    {
        var province = _stateModel.ProvinceNodes[0];
        var leafValueList = province.LeafValueContents.ToList();

        Multiple(() =>
        {
            That(province, Is.Not.Null);
            That(province.Key, Is.EqualTo("provinces"));
            That(province.Position.Line, Is.EqualTo(35));
        });
        That(leafValueList, Has.Count.EqualTo(3));
        That(leafValueList[0].ValueText, Is.EqualTo("10"));
        That(leafValueList[0].Position.Line, Is.EqualTo(36));
        That(leafValueList[1].ValueText, Is.EqualTo("20"));
        That(leafValueList[1].Position.Line, Is.EqualTo(36));
        That(leafValueList[2].ValueText, Is.EqualTo("30"));
        That(leafValueList[2].Position.Line, Is.EqualTo(36));
    }

    [Test]
    public void StateModelResourceTest()
    {
        var resource = _stateModel.ResourceNodes[0];
        var leafContents = resource.Leaves.ToList();

        Multiple(() =>
        {
            That(resource, Is.Not.Null);
            That(resource.Key, Is.EqualTo("resources"));
            That(resource.Position.Line, Is.EqualTo(8));
        });
        That(leafContents, Has.Count.EqualTo(1));
        That(leafContents[0].Key, Is.EqualTo("oil"));
        That(leafContents[0].ValueText, Is.EqualTo("1"));
        That(leafContents[0].Position.Line, Is.EqualTo(9));
    }

    [Test]
    [Ignore("对 Buildings 的解析要改")]
    public void StateModelBuildingsTest()
    {
        var building = _stateModel.Buildings[0];

        Multiple(() =>
        {
            That(building, Is.Not.Null);
            That(building.Key, Is.EqualTo("buildings"));
            //That(building.Leaves, Is.EqualTo("buildings"));
            That(building.Position.Line, Is.EqualTo(16));
        });
    }

    [Test]
    public void EmptyFileModelTest()
    {
        var emptyNode = new CWToolsParser(Path.Combine(PathManager.TestFolderPath, "EmptyFile.txt")).GetResult();
        var emptyModel = new StateFileAnalyzer.StateModel(emptyNode);

        Multiple(() =>
        {
            That(emptyModel.Ids, Has.Count.Zero);
            That(emptyModel.Names, Has.Count.Zero);
            That(emptyModel.Owners, Has.Count.Zero);
            That(emptyModel.HasCoreTags, Has.Count.Zero);
            That(emptyModel.Manpowers, Has.Count.Zero);
            That(emptyModel.StateCategories, Has.Count.Zero);
            That(emptyModel.VictoryPointNodes, Has.Count.Zero);
            That(emptyModel.ProvinceNodes, Has.Count.Zero);
            That(emptyModel.Buildings, Has.Count.Zero);
            That(emptyModel.BuildingsByProvince, Has.Count.Zero);
            That(emptyModel.ResourceNodes, Has.Count.Zero);
            That(emptyModel.IsEmptyFile, Is.True);
        });
    }
}
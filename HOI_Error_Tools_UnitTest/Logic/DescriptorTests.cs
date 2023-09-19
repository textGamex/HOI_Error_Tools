using HOI_Error_Tools.Logic;
using HOI_Error_Tools.Logic.Game;

namespace HOI_Error_Tools_UnitTest.Logic;

[TestFixture]
[TestOf(typeof(Descriptor))]
public class DescriptorTests
{
    [Test]
    public void CreateDescriptorTest()
    {
        var descriptor = new Descriptor(Path.Combine(PathManager.ModRootPath, "descriptor.mod"));
        var expectedTags = new [] { "National Focuses", "Fixes", "Events" };

        Multiple(() =>
        {
            That(descriptor.Name, Is.EqualTo("TestName"));
            That(descriptor.Picture, Is.Null);
            That(descriptor.Tags, Is.EquivalentTo(expectedTags));
            That(descriptor.Version, Is.EqualTo("1.0.0"));
            That(descriptor.SupportedVersion, Is.EqualTo("1.11.*"));
            That(descriptor.PictureName, Is.EqualTo("thumbnail.png"));
            That(descriptor.RemoteFileId, Is.Empty);
            That(descriptor.ReplacePaths, Has.Count.Zero);
        });
    }

    [Test]
    [Ignore("服务不加载, Log 为 null")]
    public void CreateFailTest()
    {
        var descriptor = new Descriptor(Path.Combine(PathManager.ModRootPath, "errorDescriptor.mod"));

        Multiple(() =>
        {
            That(descriptor.Name, Is.EqualTo(string.Empty));
            That(descriptor.Picture, Is.Null);
            That(descriptor.Tags, Is.Empty);
            That(descriptor.Version, Is.EqualTo(string.Empty));
            That(descriptor.SupportedVersion, Is.EqualTo(string.Empty));
            That(descriptor.PictureName, Is.EqualTo(string.Empty));
            That(descriptor.RemoteFileId, Is.EqualTo(string.Empty));
            That(descriptor.ReplacePaths, Has.Count.Zero);
        });
    }
}
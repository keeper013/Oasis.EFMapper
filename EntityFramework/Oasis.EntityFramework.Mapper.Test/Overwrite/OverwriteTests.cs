namespace Oasis.EntityFramework.Mapper.Test.Overwrite;

using NUnit.Framework;

public sealed class OverwriteTests : TestBase
{
    [Test]
    public void TestWriteToExistingObject()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .RegisterTwoWay<OverwriteSourceOuter, OverwriteTargetOuter>()
            .Build()
            .MakeToMemoryMapper();

        var source = new OverwriteSourceOuter
        {
            A = 1,
            Inner = new OverwriteSourceInner
            {
                C = 2,
            },
        };

        var target = new OverwriteTargetOuter
        {
            A = 3,
            B = 4,
            Inner = new OverwriteTargetInner
            {
                C = 5,
                D = 6,
            },
        };

        // act
        mapper.Map(source, target);

        // assert
        Assert.AreEqual(1, target.A);
        Assert.AreEqual(4, target.B);
        Assert.AreEqual(2, target.Inner.C);
        Assert.AreEqual(6, target.Inner.D);
    }
}

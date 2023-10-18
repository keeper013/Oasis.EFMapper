namespace Oasis.EntityFrameworkCore.Mapper.Test.Overwrite;

using Xunit;

public sealed class OverwriteTests : TestBase
{
    [Fact]
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
        Assert.Equal(1, target.A);
        Assert.Equal(4, target.B);
        Assert.Equal(2, target.Inner.C);
        Assert.Equal(6, target.Inner.D);
    }
}

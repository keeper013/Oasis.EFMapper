namespace Oasis.EfMapper.Test;

using Microsoft.EntityFrameworkCore;
using Xunit;

public class MapScalarPropertiesTest
{
    private readonly DbContext _dbContext;

    public MapScalarPropertiesTest()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(this.GetType().Name)
            .Options;
        _dbContext = new DatabaseContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public void MapScalarProperties_ShouldWork()
    {
        // arrange
        var factory = new EfMapperFactory();
        var mapperBuilder = factory.Make(this.GetType().Name);
        mapperBuilder.Register<Class1, Class2>();
        mapperBuilder.Register<Class2, Class3>();
        mapperBuilder.Register<Class3, Class1>();
        var mapper = mapperBuilder.Build();

        var instance1_1 = InitializeClass1Instance();
        var instance2_1 = InitializeClass2Instance();
        mapper.Map(instance1_1, instance2_1, _dbContext);

        // assert
        Assert.Equal(instance1_1.X1, instance2_1.X1);
        Assert.Equal(0, instance2_1.X2);
        Assert.NotEqual(instance1_1.X3, instance2_1.X3);
        Assert.Equal(instance1_1.Y1, instance2_1.Y1);
        Assert.Equal("0", instance2_1.Y3);
        Assert.Null(instance2_1.Z);

        // act
        var instance2_2 = InitializeClass2Instance();
        var instance3_2 = InitializeClass3Instance();
        mapper.Map<Class2, Class3>(instance2_2, instance3_2, _dbContext);

        // assert
        Assert.Equal(instance2_2.Y3, instance3_2.Y3);
        Assert.NotEqual(instance2_2.X3, instance3_2.X3);
        Assert.Equal("21", instance3_2.Y2);

        // act
        var instance3_3 = InitializeClass3Instance();
        var instance1_3 = InitializeClass1Instance();
        mapper.Map<Class3, Class1>(instance3_3, instance1_3, _dbContext);

        // assert
        Assert.Equal(instance3_3.X3, instance1_3.X3);
        Assert.Equal(instance3_3.Y2, instance1_3.Y2);
        Assert.Equal(1, instance1_3.X1);
        Assert.Equal("1", instance1_3.Y1);
        Assert.Single(instance1_3.Z!);
    }

    private Class1 InitializeClass1Instance()
    {
        return new Class1
        {
            Id = 1,
            X1 = 1,
            X3 = 3,
            Y1 = "1",
            Y2 = "2",
            Z = new byte[1],
        };
    }

    private Class2 InitializeClass2Instance()
    {
        return new Class2
        {
            Id = 2,
            X1 = 0,
            X2 = 0,
            X3 = 0,
            Y1 = "0",
            Y3 = "0",
            Z = null,
        };
    }

    private Class3 InitializeClass3Instance()
    {
        return new Class3
        {
            X3 = 31,
            Y2 = "21",
            Y3 = "31",
        };
    }
}

public sealed class Class1 : EntityBase
{
    public int X1 { get; set; }

    public int X3 { get; set; }

    public string? Y1 { get; set; }

    public string? Y2 { get; set; }

    public byte[]? Z { get; set; }
}

public sealed class Class2 : EntityBase
{
    public int X1 { get; set; }

    public int X2 { get; set; }

    public long X3 { get; set; }

    public string? Y1 { get; set; }

    public string? Y3 { get; set; }

    public byte[]? Z { get; set; }
}

public class Class3 : EntityBase
{
    public int X3 { get; set; }

    public string? Y2 { get; set; }

    public string? Y3 { get; set; }
}

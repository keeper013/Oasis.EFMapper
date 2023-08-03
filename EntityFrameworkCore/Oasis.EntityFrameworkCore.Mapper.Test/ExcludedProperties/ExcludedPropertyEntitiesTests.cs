namespace Oasis.EntityFrameworkCore.Mapper.Test.ExcludedProperties;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using Xunit;

public sealed class ExcludedPropertyEntitiesTests : TestBase
{
    [Fact]
    public void Exclude_CustomMappedProperties_ShouldThrowException()
    {
        // arrange act & assert
        Assert.Throws<CustomMappingPropertyExcludedException>(() =>
            MakeDefaultMapperBuilder().Configure<ExcludedPropertyEntity2, ExcludedPropertyEntity1>()
                .MapProperty(e1 => e1.IntProp, e2 => e2.IntProp + 1)
                .ExcludePropertiesByName("IntProp")
                .Finish());
    }

    [Fact]
    public void Exclude_IdForType_ShouldThrowException()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder(new string[] { nameof(EntityBase.Id) }).Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>();

        // act & assert
        Assert.Throws<KeyTypeExcludedException>(() => mapperBuilder.Build());
    }

    [Fact]
    public void Exclude_NonExistingPropertiesForType_ShouldThrowException()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();

        // act & assert
        Assert.Throws<UselessExcludeException>(() => mapperBuilder.Configure<ExcludedPropertyEntity2>().ExcludedPropertiesByName("NonExistProperty"));
    }

    [Fact]
    public void Exclude_NonExistingPropertiesForMapping_ShouldThrowException()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();

        // act & assert
        Assert.Throws<UselessExcludeException>(() => mapperBuilder.Configure<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().ExcludePropertiesByName("NonExistProperty"));
    }

    [Fact]
    public void Exclude_GlobalProperties_ShouldSucceed()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder(new string[] { nameof(ExcludedPropertyEntity1.StringProp) });
        var mapper = mapperBuilder.Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.Equal(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }

    [Fact]
    public void Exclude_SourceTypeProperties_ShouldSucceed()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        var mapper = mapperBuilder
            .Configure<ExcludedPropertyEntity2>()
                .ExcludedPropertiesByName(nameof(ExcludedPropertyEntity2.StringProp))
                .Finish()
            .Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.Equal(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }

    [Fact]
    public void Exclude_TargetTypeProperties_ShouldSucceed()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        var mapper = mapperBuilder
            .Configure<ExcludedPropertyEntity1>()
                .ExcludedPropertiesByName(nameof(ExcludedPropertyEntity1.StringProp))
                .Finish()
            .Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.Equal(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }

    [Fact]
    public void Exclude_MappingProperties_ShouldSucceed()
    {
        // arrange
        var mapperBuilder = MakeDefaultMapperBuilder();
        var mapper = mapperBuilder
            .Configure<ExcludedPropertyEntity2, ExcludedPropertyEntity1>()
                .ExcludePropertiesByName(nameof(ExcludedPropertyEntity1.StringProp))
                .Finish()
            .Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.Equal(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }
}

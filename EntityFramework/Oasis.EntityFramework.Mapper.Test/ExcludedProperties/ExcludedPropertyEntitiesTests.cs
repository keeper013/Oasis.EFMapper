namespace Oasis.EntityFramework.Mapper.Test.ExcludedProperties;

using Oasis.EntityFramework.Mapper.Exceptions;
using NUnit.Framework;

public sealed class ExcludedPropertyEntitiesTests : TestBase
{
    [Test]
    public void Exclude_CustomMappedProperties_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var config = factory.MakeCustomTypeMapperBuilder<ExcludedPropertyEntity2, ExcludedPropertyEntity1>()
            .MapProperty(e1 => e1.IntProp, e2 => e2.IntProp + 1)
            .ExcludePropertyByName("IntProp");

        // act & assert
        Assert.Throws<CustomMappingPropertyExcludedException>(() => mapperBuilder.Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(config.Build()));
    }

    [Test]
    public void Exclude_IdForType_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.WithConfiguration<ExcludedPropertyEntity2>(new EntityConfiguration { excludedProperties = new[] { nameof(EntityBase.Id) } });
        mapperBuilder.Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>();

        // act & assert
        Assert.Throws<KeyTypeExcludedException>(() => mapperBuilder.Build());
    }

    [Test]
    public void Exclude_NonExistingPropertiesForType_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act & assert
        Assert.Throws<UselessExcludeException>(() => mapperBuilder.WithConfiguration<ExcludedPropertyEntity2>(new EntityConfiguration { excludedProperties = new[] { "NonExistProperty" } }));
    }

    [Test]
    public void Exclude_NonExistingPropertiesForMapping_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var config = factory.MakeCustomTypeMapperBuilder<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().ExcludePropertyByName("NonExistProperty");

        // act & assert
        Assert.Throws<UselessExcludeException>(() => mapperBuilder.Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(config.Build()));
    }

    [Test]
    public void Exclude_SourceTypeProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var mapper = mapperBuilder
            .WithConfiguration<ExcludedPropertyEntity2>(new EntityConfiguration { excludedProperties = new[] { nameof(ExcludedPropertyEntity2.StringProp) } })
            .Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.AreEqual(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }

    [Test]
    public void Exclude_TargetTypeProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var mapper = mapperBuilder
            .WithConfiguration<ExcludedPropertyEntity1>(new EntityConfiguration { excludedProperties = new[] { nameof(ExcludedPropertyEntity1.StringProp) } })
            .Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.AreEqual(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }

    [Test]
    public void Exclude_MappingProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var config = factory.MakeCustomTypeMapperBuilder<ExcludedPropertyEntity2, ExcludedPropertyEntity1>().ExcludePropertyByName(nameof(ExcludedPropertyEntity1.StringProp));
        var mapper = mapperBuilder.Register<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(config.Build()).Build();

        // act
        var entity2 = new ExcludedPropertyEntity2
        {
            IntProp = 1,
            StringProp = "test",
        };

        var entity1 = mapper.Map<ExcludedPropertyEntity2, ExcludedPropertyEntity1>(entity2);
        Assert.AreEqual(entity2.IntProp, entity1.IntProp);
        Assert.Null(entity1.StringProp);
    }
}

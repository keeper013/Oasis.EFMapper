namespace Oasis.EntityFramework.Mapper.Test.Custom;

using NUnit.Framework;
using Oasis.EntityFramework.Mapper.Exceptions;

[TestFixture]
public class CustomPropertyMappingTests
{
    public CustomPropertyMappingTests()
    {
        DefaultConfiguration = new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.Timestamp));
    }

    private TypeConfiguration DefaultConfiguration { get; }

    [Test]
    public void TestRegisterExistingMapperWithoutCustomPropertyMapping()
    {
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<CustomEntity1Wrapper, CustomEntity2Wrapper>();
        var custom = factory.MakeCustomPropertyMapperBuilder<CustomEntity1, CustomEntity2>()
            .MapProperty(c2 => c2.InternalIntProperty, c1 => c1.InternalProperty.IntProperty)
            .Build();
        Assert.Throws<MapperExistsException>(() => mapperBuilder.Register(custom));
    }

    [Test]
    public void TestMappingCustomProperty()
    {
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var custom = factory.MakeCustomPropertyMapperBuilder<CustomEntity1, CustomEntity2>()
            .MapProperty(c2 => c2.InternalIntProperty, c1 => c1.InternalProperty.IntProperty)
            .Build();
        var mapper = mapperBuilder.Register(custom).Build();

        var c1 = new CustomEntity1
        {
            StringProperty = "a",
            InternalProperty = new InterEntity1 { IntProperty = 100 },
        };

        var c2 = mapper.Map<CustomEntity1, CustomEntity2>(c1);
        Assert.AreEqual("a", c2.StringProperty);
        Assert.AreEqual(100, c2.InternalIntProperty);
    }

    [Test]
    public void TestMappingCustomPropertyOverridingOriginal()
    {
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        var custom = factory.MakeCustomPropertyMapperBuilder<CustomEntity3, CustomEntity2>()
            .MapProperty(c2 => c2.InternalIntProperty, c3 => c3.InternalProperty.IntProperty)
            .Build();
        var mapper = mapperBuilder.Register(custom).Build();

        var c3 = new CustomEntity3
        {
            StringProperty = "a",
            InternalProperty = new InterEntity1 { IntProperty = 100 },
            InternalIntProperty = 200,
        };

        var c2 = mapper.Map<CustomEntity3, CustomEntity2>(c3);
        Assert.AreEqual("a", c2.StringProperty);
        Assert.AreEqual(100, c2.InternalIntProperty);
    }
}

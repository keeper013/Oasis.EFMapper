﻿namespace Oasis.EntityFramework.Mapper.Test.Custom;

using NUnit.Framework;

[TestFixture]
public class CustomPropertyMappingTests
{
    [Test]
    public void TestMappingCustomProperty()
    {
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .Configure<CustomEntity1, CustomEntity2>()
                .MapProperty(c2 => c2.InternalIntProperty, c1 => c1.InternalProperty.IntProperty)
                .Finish()
            .Build()
            .MakeToMemoryMapper();

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
        var mapperBuilder = MakeDefaultMapperBuilder(factory);
        var mapper = mapperBuilder
            .Configure<CustomEntity3, CustomEntity2>()
                .MapProperty(c2 => c2.InternalIntProperty, c3 => c3.InternalProperty.IntProperty)
                .Finish()
            .Build()
            .MakeToMemoryMapper();

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

    private static IMapperBuilder MakeDefaultMapperBuilder(IMapperBuilderFactory factory)
    {
        return factory
            .Configure()
                .SetKeyPropertyNames(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken))
                .Finish()
            .MakeMapperBuilder();
    }
}

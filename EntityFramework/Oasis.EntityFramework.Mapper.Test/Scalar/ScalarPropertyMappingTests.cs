﻿namespace Oasis.EntityFramework.Mapper.Test.Scalar;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Data.Entity;

public sealed class ScalarPropertyMappingTests : TestBase
{
    [Test]
    public async Task MapNew_ShouldSucceed()
    {
        // arrange
        var factory = MakeDefaultMapperBuilder().RegisterTwoWay<ScalarEntity2, ScalarEntity1>().Build();
        var byteArray = new byte[] { 2, 3, 4 };
        var instance = new ScalarEntity2(2, 3, "4", byteArray);

        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var mapper = factory.MakeToDatabaseMapper(databaseContext);
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        });

        ScalarEntity1? entity = null;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = (await databaseContext.Set<ScalarEntity1>().FirstOrDefaultAsync())!;
        });

        // assert
        Assert.NotNull(entity);
        Assert.AreEqual(2, entity!.IntProp);
        Assert.AreEqual(3, entity.LongNullableProp);
        Assert.AreEqual("4", entity.StringProp);
        Assert.AreEqual(byteArray, entity.ByteArrayProp);
    }

    [Test]
    public async Task MapScalarProperties_ValidProperties_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().RegisterTwoWay<ScalarEntity1, ScalarEntity2>().Build().MakeMapper();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, ScalarEntity2>(entity);

        // assert
        Assert.AreEqual(2, instance.IntProp);
        Assert.AreEqual(3, instance.LongNullableProp);
        Assert.AreEqual("4", instance.StringProp);
        Assert.AreEqual(byteArray, instance.ByteArrayProp);

        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        ScalarEntity1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            result = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, null);
        });

        // assert
        Assert.AreEqual(1, result.IntProp);
        Assert.AreEqual(2, result.LongNullableProp);
        Assert.AreEqual("3", result.StringProp);
        Assert.AreEqual(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Test]
    public async Task MapNewWithoutIdToDatabase_ShouldInsert()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ScalarEntityNoBase1, ScalarEntity1>().Build().MakeToDatabaseMapper();
        var instance = new ScalarEntityNoBase1(2, 3, "4", new byte[] { 2, 3, 4 });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            _ = await mapper.MapAsync<ScalarEntityNoBase1, ScalarEntity1>(instance, null);
            _ = await databaseContext.SaveChangesAsync();
        });

        int count = 0;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            count = await databaseContext.Set<ScalarEntity1>().CountAsync();
        });

        Assert.AreEqual(1, count);
    }

    [Test]
    public async Task MapNewWithIdToDatabase_ShouldInsert()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().RegisterTwoWay<ScalarEntity2, ScalarEntity1>().Build().MakeToDatabaseMapper();
        var byteArray = new byte[] { 2, 3, 4 };
        var instance = new ScalarEntity2(2, 3, "4", byteArray)
        {
            Id = 100,
        };

        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        });

        ScalarEntity1? entity = null;
        int count = 0;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            count = await databaseContext.Set<ScalarEntity1>().CountAsync();
            entity = (await databaseContext.Set<ScalarEntity1>().FirstOrDefaultAsync())!;
        });

        // assert
        Assert.AreEqual(1, count);
        Assert.NotNull(entity);

        // if it's entity framework 6.0, the value will be 1 instead of mapped 100 here.
        Assert.AreEqual(1, entity!.Id);
        Assert.NotNull(entity.ConcurrencyToken);
        Assert.AreEqual(2, entity.IntProp);
        Assert.AreEqual(3, entity.LongNullableProp);
        Assert.AreEqual("4", entity.StringProp);
        Assert.AreEqual(byteArray, entity.ByteArrayProp);
    }

    [Test]
    public async Task MapExistingWithIdToDatabase_ShouldUpdate()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().RegisterTwoWay<ScalarEntity2, ScalarEntity1>().Build().MakeToDatabaseMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 });
            databaseContext.Set<ScalarEntity1>().Add(entity);
            _ = await databaseContext.SaveChangesAsync();
        });

        long id = 0;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await databaseContext.Set<ScalarEntity1>().FirstOrDefaultAsync();
            id = entity!.Id;
        });

        var byteArray = new byte[] { 2, 3, 4 };
        var instance = new ScalarEntity2(2, 3, "4", byteArray)
        {
            Id = id,
        };

        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, null);
            await databaseContext.SaveChangesAsync();
        });

        ScalarEntity1? entity = null;
        int count = 0;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            count = await databaseContext.Set<ScalarEntity1>().CountAsync();
            entity = (await databaseContext.Set<ScalarEntity1>().FirstOrDefaultAsync())!;
        });

        // assert
        Assert.NotNull(entity);
        Assert.AreEqual(1, count);
        Assert.AreEqual(id, entity!.Id);
        Assert.NotNull(entity.ConcurrencyToken);
        Assert.AreEqual(2, entity.IntProp);
        Assert.AreEqual(3, entity.LongNullableProp);
        Assert.AreEqual("4", entity.StringProp);
        Assert.AreEqual(byteArray, entity.ByteArrayProp);
    }

    [Test]
    public async Task RegisterTarget_WithFactoryMethod_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithFactoryMethod(() => new EntityWithoutDefaultConstructor(100))
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>()
            .Build()
            .MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", new byte[] { 2, 3, 4 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, EntityWithoutDefaultConstructor>(entity);

        // assert
        Assert.AreEqual(2, instance.IntProp);
    }

    [Test]
    public async Task MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ScalarEntity1, ScalarEntity3>().Build().MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result = mapper.Map<ScalarEntity1, ScalarEntity3>(entity);

        // assert
        Assert.NotNull(result.IntProp);
        Assert.AreEqual(0, result.LongNullableProp);
        Assert.Null(result.StringProp1);
        Assert.Null(result.ByteArrayProp);
    }

    [Test]
    public async Task ConvertWithoutStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder().Register<ScalarEntity1, ScalarEntity4>().Build().MakeToMemoryMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().FirstAsync();
        });

        var result = mapper.Map<ScalarEntity1, ScalarEntity4>(entity);

        // assert
        Assert.Null(result.ByteArrayProp);
    }

    [Test]
    public async Task ConvertWithLambdaExpressionScalarMapper_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter((ByteArrayWrapper? wrapper) => wrapper!.Bytes)
            .WithScalarConverter((byte[]? array) => new ByteArrayWrapper(array!))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>()
            .Build()
            .MakeMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, default, "abc", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result1 = mapper.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });

        ScalarEntity1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            result2 = await mapper.MapAsync<ScalarEntity4, ScalarEntity1>(result1, null);
        });

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes!, result2.ByteArrayProp!));
    }

    [Test]
    public async Task ConvertWithStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter((ByteArrayWrapper? wrapper) => ByteArrayWrapper.ConvertStatic(wrapper!))
            .WithScalarConverter((byte[]? array) => ByteArrayWrapper.ConvertStatic(array!))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>()
            .Build()
            .MakeMapper();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, default, "abc", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result1 = mapper.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });

        ScalarEntity1 result2 = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            result2 = await mapper.MapAsync<ScalarEntity4, ScalarEntity1>(result1, null);
        });

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes!, result2.ByteArrayProp!));
    }

    [Test]
    public async Task MapScalarProperties_ToEntityNoId_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Register<ScalarEntity1, ScalarEntityNoBase1>()
            .Register<ScalarEntityNoBase1, ScalarEntityNoBase2>()
            .Build()
            .MakeToMemoryMapper();
        var byteArray = new byte[] { 2, 3, 4 };

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        mapper.StartSession();
        var instance1 = mapper.Map<ScalarEntity1, ScalarEntityNoBase1>(entity);

        // assert
        Assert.AreEqual(2, instance1!.IntProp);
        Assert.AreEqual(3, instance1.LongNullableProp);
        Assert.AreEqual("4", instance1.StringProp);
        Assert.AreEqual(byteArray, instance1.ByteArrayProp);

        var instance2 = mapper.Map<ScalarEntityNoBase1, ScalarEntityNoBase2>(instance1);
        mapper.StopSession();

        // assert
        Assert.AreEqual(2, instance2!.IntProp);
        Assert.AreEqual(3, instance2.LongNullableProp);
        Assert.AreEqual("4", instance2.StringProp);
        Assert.AreEqual(byteArray, instance2.ByteArrayProp);
    }

    [Test]
    public async Task MapScalarProperties_CustomKeyProperties_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .Configure<ScalarEntityCustomKeyProperties1>()
                .SetKeyPropertyNames(nameof(EntityBase.ConcurrencyToken), nameof(EntityBase.Id))
                .Finish()
            .Configure<ScalarEntityNoConcurrencyToken1>()
                .SetIdentityPropertyName(nameof(EntityBaseNoConcurrencyToken.AnotherId))
                .Finish()
            .Register<ScalarEntity1, ScalarEntityCustomKeyProperties1>()
            .Register<ScalarEntity1, ScalarEntityNoConcurrencyToken1>()
            .Build()
            .MakeToMemoryMapper();

        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        mapper.StartSession();
        var instance1 = mapper.Map<ScalarEntity1, ScalarEntityCustomKeyProperties1>(entity);

        // assert
        Assert.AreNotEqual(default, instance1.ConcurrencyToken);
        Assert.AreEqual(2, instance1.IntProp);
        Assert.AreEqual(3, instance1.LongNullableProp);
        Assert.AreEqual("4", instance1.StringProp);
        Assert.AreEqual(byteArray, instance1.ByteArrayProp);

        var instance2 = mapper.Map<ScalarEntity1, ScalarEntityNoConcurrencyToken1>(entity!);
        mapper.StopSession();
        Assert.AreNotEqual(default, instance2.AnotherId);
        Assert.AreEqual(2, instance2.IntProp);
        Assert.AreEqual(3, instance2.LongNullableProp);
        Assert.AreEqual("4", instance2.StringProp);
        Assert.AreEqual(byteArray, instance2.ByteArrayProp);
    }

    [Test]
    public async Task MapScalarProperties_WrappedKeyProperties_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<byte[], ByteArrayWrapper>(arr => new ByteArrayWrapper(arr!))
            .WithScalarConverter<ByteArrayWrapper, byte[]>(wrapper => wrapper!.Bytes!)
            .WithScalarConverter<long, LongWrapper>(l => new LongWrapper(l))
            .WithScalarConverter<LongWrapper, long>(wrapper => wrapper.Value)
            .Configure<WrappedScalarEntity2>()
                .SetKeyPropertyNames(nameof(WrappedScalarEntity2.WrappedId), nameof(WrappedScalarEntity2.WrappedConcurrencyToken))
                .Finish()
            .RegisterTwoWay<ScalarEntity1, WrappedScalarEntity2>()
            .Register<WrappedScalarEntity2, WrappedScalarEntity2>()
            .Build()
            .MakeMapper();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance1 = mapper.Map<ScalarEntity1, WrappedScalarEntity2>(entity);

        instance1.IntProp = 1;
        instance1.LongNullableProp = 2;
        instance1.StringProp = "3";
        instance1.ByteArrayProp = new byte[] { 1, 2, 3 };

        var instance2 = mapper.Map<WrappedScalarEntity2, WrappedScalarEntity2>(instance1);

        // assert
        Assert.AreEqual(1, instance2!.IntProp);
        Assert.AreEqual(2, instance2.LongNullableProp);
        Assert.AreEqual("3", instance2.StringProp);
        Assert.AreEqual(instance1.ByteArrayProp, instance2.ByteArrayProp);

        ScalarEntity1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            result = await mapper.MapAsync<WrappedScalarEntity2, ScalarEntity1>(instance2, null);
        });

        // assert
        Assert.AreEqual(1, result.IntProp);
        Assert.AreEqual(2, result.LongNullableProp);
        Assert.AreEqual("3", result.StringProp);
        Assert.AreEqual(instance2.ByteArrayProp, result.ByteArrayProp);
    }

    [Test]
    public async Task MapScalarProperties_PrimitiveToNullable_ShouldSucceed()
    {
        // arrange
        var mapper = MakeDefaultMapperBuilder()
            .WithScalarConverter<int, int?>(i => i)
            .WithScalarConverter<int?, int>(ni => ni.HasValue ? ni.Value : 0)
            .WithScalarConverter<long, long?>(l => l)
            .WithScalarConverter<long?, long>(nl => nl.HasValue ? nl.Value : 0)
            .RegisterTwoWay<ScalarEntity1, ScalarEntity5>()
            .Build()
            .MakeMapper();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, null, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1 entity = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, ScalarEntity5>(entity);

        // assert
        Assert.AreEqual(2, instance.IntProp);
        Assert.AreEqual(0, instance.LongNullableProp);
        Assert.AreEqual("4", instance.StringProp);
        Assert.AreEqual(byteArray, instance.ByteArrayProp);

        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        ScalarEntity1 result = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            result = await mapper.MapAsync<ScalarEntity5, ScalarEntity1>(instance, null);
        });

        // assert
        Assert.AreEqual(1, result.IntProp);
        Assert.AreEqual(2, result.LongNullableProp);
        Assert.AreEqual("3", result.StringProp);
        Assert.AreEqual(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Test]
    public void ConvertWithDuplicatedScalarMapper_ShouldFail()
    {
        // arrange, act and assert
        Assert.Throws<ScalarMapperExistsException>(() => MakeDefaultMapperBuilder()
            .WithScalarConverter<ByteArrayWrapper, byte[]>((wrapper) => ByteArrayWrapper.ConvertStatic(wrapper!)!)
            .WithScalarConverter<ByteArrayWrapper, byte[]>((wrapper) => wrapper!.Bytes!, true));
    }

    [Test]
    public void RegisterTarget_WithoutDefaultConstructorAndNoFactoryMethod_ShouldFail()
    {
        // arrange, act & assert
        Assert.Throws<FactoryMethodException>(() => MakeDefaultMapperBuilder().Register<ScalarEntity1, EntityWithoutDefaultConstructor>().Build());
    }

    [Test]
    public void RegisterTarget_WithDefaultConstructorAndFactoryMethod_ShouldFail()
    {
        // arrange, act & assert
        Assert.Throws<FactoryMethodException>(() => MakeDefaultMapperBuilder()
            .WithFactoryMethod(() => new ScalarEntity1())
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Test]
    public void RegisterTarget_WithDuplicatedFactoryMethod_ShouldFail()
    {
        // arrange, act & assert
        Assert.Throws<FactoryMethodExistsException>(() => MakeDefaultMapperBuilder()
            .WithFactoryMethod(() => new EntityWithoutDefaultConstructor(1))
            .WithFactoryMethod(() => new EntityWithoutDefaultConstructor(2), true)
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Test]
    public void RegisterTarget_WithDuplicatedConfig_ShouldFail()
    {
        // arrange
        var builder = new MapperBuilderFactory()
            .Configure()
            .SetThrowForRedundantConfiguration(true)
            .Finish()
            .MakeMapperBuilder();

        // act & assert
        Assert.Throws<RedundantConfigurationException>(() => builder
            .Configure<EntityWithoutDefaultConstructor>()
                .SetKeyPropertyNames(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken))
                .Finish()
            .Configure<EntityWithoutDefaultConstructor>()
                .SetKeyPropertyNames(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken))
                .Finish()
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Test]
    [TestCase(TestEnum.Value1)]
    [TestCase(TestEnum.Value2)]
    [TestCase(TestEnum.Value3)]
    public async Task MapEnum_ShouldSucceed(TestEnum input)
    {
        var mapper = MakeDefaultMapperBuilder()
            .Register<EnumEntity2, EnumEntity1>()
            .Build()
            .MakeToDatabaseMapper();

        var enumEntity = new EnumEntity2 { EnumProperty = input };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            _ = await mapper.MapAsync<EnumEntity2, EnumEntity1>(enumEntity, null);
            await databaseContext.SaveChangesAsync();
            var entity = await databaseContext.Set<EnumEntity1>().FirstOrDefaultAsync();
            Assert.NotNull(entity);
            Assert.AreEqual(input, entity!.EnumProperty);
        });
    }

    [Theory]
    [TestCase(null)]
    [TestCase(TestEnum.Value1)]
    [TestCase(TestEnum.Value2)]
    [TestCase(TestEnum.Value3)]
    public async Task MapNullableEnum_ShouldSucceed(TestEnum? input)
    {
        var mapper = MakeDefaultMapperBuilder()
            .Register<NullableEnumEntity2, NullableEnumEntity1>()
            .Build()
            .MakeToDatabaseMapper();

        var enumEntity = new NullableEnumEntity2 { EnumProperty = input };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mapper.DatabaseContext = databaseContext;
            _ = await mapper.MapAsync<NullableEnumEntity2, NullableEnumEntity1>(enumEntity, null);
            await databaseContext.SaveChangesAsync();
            var entity = await databaseContext.Set<NullableEnumEntity1>().FirstOrDefaultAsync();
            Assert.NotNull(entity);
            Assert.AreEqual(input, entity!.EnumProperty);
        });
    }
}

namespace Oasis.EntityFrameworkCore.Mapper.Test.Scalar;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class ScalarPropertyMappingTests : TestBase
{
    [Fact]
    public async Task MapNew_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ScalarEntity2, ScalarEntity1>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };
        var instance = new ScalarEntity2(2, 3, "4", byteArray);

        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, databaseContext);
            await databaseContext.SaveChangesAsync();
        });

        ScalarEntity1? entity = null;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = (await databaseContext.Set<ScalarEntity1>().FirstOrDefaultAsync())!;
        });

        // assert
        Assert.NotNull(entity);
        Assert.Equal(2, entity!.IntProp);
        Assert.Equal(3, entity.LongNullableProp);
        Assert.Equal("4", entity.StringProp);
        Assert.Equal(byteArray, entity.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_ValidProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ScalarEntity1, ScalarEntity2>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, ScalarEntity2>(entity!);

        // assert
        Assert.Equal(2, instance.IntProp);
        Assert.Equal(3, instance.LongNullableProp);
        Assert.Equal("4", instance.StringProp);
        Assert.Equal(byteArray, instance.ByteArrayProp);

        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        ScalarEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, databaseContext);
        });

        // assert
        Assert.Equal(1, result!.IntProp);
        Assert.Equal(2, result.LongNullableProp);
        Assert.Equal("3", result.StringProp);
        Assert.Equal(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Fact]
    public async Task MapNewWithoutIdToDatabase_ShouldInsert()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .Register<ScalarEntityNoBase1, ScalarEntity1>();
        var mapper = mapperBuilder.Build();
        var instance = new ScalarEntityNoBase1(2, 3, "4", new byte[] { 2, 3, 4 });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            _ = await mapper.MapAsync<ScalarEntityNoBase1, ScalarEntity1>(instance, databaseContext);
            _ = await databaseContext.SaveChangesAsync();
        });

        int count = 0;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            count = await databaseContext.Set<ScalarEntity1>().CountAsync();
        });

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task MapNewWithIdToDatabase_ShouldInsert()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ScalarEntity2, ScalarEntity1>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };
        var instance = new ScalarEntity2(2, 3, "4", byteArray)
        {
            Id = 100,
        };

        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, databaseContext);
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
        Assert.Equal(1, count);
        Assert.NotNull(entity);

        // if it's entity framework 6.0, the value will be 1 instead of mapped 100 here.
        Assert.Equal(100, entity!.Id);
        Assert.NotNull(entity.Timestamp);
        Assert.Equal(2, entity.IntProp);
        Assert.Equal(3, entity.LongNullableProp);
        Assert.Equal("4", entity.StringProp);
        Assert.Equal(byteArray, entity.ByteArrayProp);
    }

    [Fact]
    public async Task MapExistingWithIdToDatabase_ShouldUpdate()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.RegisterTwoWay<ScalarEntity2, ScalarEntity1>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var entity = new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 });
            _ = await databaseContext.Set<ScalarEntity1>().AddAsync(entity);
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
            var entity = await mapper.MapAsync<ScalarEntity2, ScalarEntity1>(instance, databaseContext);
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
        Assert.Equal(1, count);
        Assert.Equal(id, entity!.Id);
        Assert.NotNull(entity.Timestamp);
        Assert.Equal(2, entity.IntProp);
        Assert.Equal(3, entity.LongNullableProp);
        Assert.Equal("4", entity.StringProp);
        Assert.Equal(byteArray, entity.ByteArrayProp);
    }

    [Fact]
    public async Task RegisterTarget_WithFactoryMethod_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithFactoryMethod<EntityWithoutDefaultConstructor>(() => new EntityWithoutDefaultConstructor(100))
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", new byte[] { 2, 3, 4 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, EntityWithoutDefaultConstructor>(entity!);

        // assert
        Assert.Equal(2, instance.IntProp);
    }

    [Fact]
    public async Task MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity3>();
        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result = mapper.Map<ScalarEntity1, ScalarEntity3>(entity!);

        // assert
        Assert.Null(result.IntProp);
        Assert.Equal(0, result.LongNullableProp);
        Assert.Null(result.StringProp1);
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task ConvertWithoutStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().FirstAsync();
        });

        var result = mapper.Map<ScalarEntity1, ScalarEntity4>(entity!);

        // assert
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task ConvertWithLambdaExpressionScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithScalarConverter((ByteArrayWrapper? wrapper) => wrapper!.Bytes)
            .WithScalarConverter((byte[]? array) => new ByteArrayWrapper(array!))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, default, "abc", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result1 = mapper.Map<ScalarEntity1, ScalarEntity4>(entity!);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });

        ScalarEntity1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await mapper.MapAsync<ScalarEntity4, ScalarEntity1>(result1, databaseContext);
        });

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes!, result2!.ByteArrayProp!));
    }

    [Fact]
    public async Task ConvertWithStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithScalarConverter((ByteArrayWrapper? wrapper) => ByteArrayWrapper.ConvertStatic(wrapper!))
            .WithScalarConverter((byte[]? array) => ByteArrayWrapper.ConvertStatic(array!))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, default, "abc", new byte[] { 1, 2, 3 }));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var result1 = mapper.Map<ScalarEntity1, ScalarEntity4>(entity!);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });

        ScalarEntity1? result2 = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result2 = await mapper.MapAsync<ScalarEntity4, ScalarEntity1>(result1, databaseContext);
        });

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes!, result2!.ByteArrayProp!));
    }

    [Fact]
    public async Task MapScalarProperties_ToEntityNoId_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .Register<ScalarEntity1, ScalarEntityNoBase1>()
            .Register<ScalarEntityNoBase1, ScalarEntityNoBase2>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var session = mapper.CreateMappingSession();
        var instance1 = session.Map<ScalarEntity1, ScalarEntityNoBase1>(entity!);

        // assert
        Assert.Equal(2, instance1!.IntProp);
        Assert.Equal(3, instance1.LongNullableProp);
        Assert.Equal("4", instance1.StringProp);
        Assert.Equal(byteArray, instance1.ByteArrayProp);

        var instance2 = session.Map<ScalarEntityNoBase1, ScalarEntityNoBase2>(instance1);

        // assert
        Assert.Equal(2, instance2!.IntProp);
        Assert.Equal(3, instance2.LongNullableProp);
        Assert.Equal("4", instance2.StringProp);
        Assert.Equal(byteArray, instance2.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_CustomKeyProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithConfiguration<ScalarEntityCustomKeyProperties1>(new TypeConfiguration(nameof(EntityBase.Timestamp), nameof(EntityBase.Id)))
            .WithConfiguration<ScalarEntityNoTimeStamp1>(new TypeConfiguration(nameof(EntityBaseNoTimeStamp.AnotherId)))
            .Register<ScalarEntity1, ScalarEntityCustomKeyProperties1>()
            .Register<ScalarEntity1, ScalarEntityNoTimeStamp1>();
        var mapper = mapperBuilder.Build();

        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var session1 = mapper.CreateMappingSession();
        var instance1 = session1.Map<ScalarEntity1, ScalarEntityCustomKeyProperties1>(entity!);

        // assert
        Assert.NotEqual(default, instance1.Timestamp);
        Assert.NotNull(instance1.Id);
        Assert.Equal(2, instance1.IntProp);
        Assert.Equal(3, instance1.LongNullableProp);
        Assert.Equal("4", instance1.StringProp);
        Assert.Equal(byteArray, instance1.ByteArrayProp);

        var instance2 = session1.Map<ScalarEntity1, ScalarEntityNoTimeStamp1>(entity!);
        Assert.NotEqual(default, instance2.AnotherId);
        Assert.Equal(2, instance2.IntProp);
        Assert.Equal(3, instance2.LongNullableProp);
        Assert.Equal("4", instance2.StringProp);
        Assert.Equal(byteArray, instance2.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_WrappedKeyProperties_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithScalarConverter<byte[], ByteArrayWrapper>(arr => new ByteArrayWrapper(arr!))
            .WithScalarConverter<ByteArrayWrapper, byte[]>(wrapper => wrapper!.Bytes!)
            .WithScalarConverter<long, LongWrapper>(l => new LongWrapper(l))
            .WithScalarConverter<LongWrapper, long>(wrapper => wrapper!.Value)
            .WithConfiguration<WrappedScalarEntity2>(new TypeConfiguration(nameof(WrappedScalarEntity2.WrappedId), nameof(WrappedScalarEntity2.WrappedTimeStamp)))
            .RegisterTwoWay<ScalarEntity1, WrappedScalarEntity2>()
            .Register<WrappedScalarEntity2, WrappedScalarEntity2>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance1 = mapper.Map<ScalarEntity1, WrappedScalarEntity2>(entity!);

        instance1.IntProp = 1;
        instance1.LongNullableProp = 2;
        instance1.StringProp = "3";
        instance1.ByteArrayProp = new byte[] { 1, 2, 3 };

        var instance2 = mapper.Map<WrappedScalarEntity2, WrappedScalarEntity2>(instance1);

        // assert
        Assert.Equal(1, instance2!.IntProp);
        Assert.Equal(2, instance2.LongNullableProp);
        Assert.Equal("3", instance2.StringProp);
        Assert.Equal(instance1.ByteArrayProp, instance2.ByteArrayProp);

        ScalarEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<WrappedScalarEntity2, ScalarEntity1>(instance2, databaseContext);
        });

        // assert
        Assert.Equal(1, result!.IntProp);
        Assert.Equal(2, result.LongNullableProp);
        Assert.Equal("3", result.StringProp);
        Assert.Equal(instance2.ByteArrayProp, result.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_PrimitiveToNullable_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);
        mapperBuilder
            .WithScalarConverter<int, int?>(i => i)
            .WithScalarConverter<int?, int>(ni => ni.HasValue ? ni.Value : 0)
            .WithScalarConverter<long, long?>(l => l)
            .WithScalarConverter<long?, long>(nl => nl.HasValue ? nl.Value : 0)
            .RegisterTwoWay<ScalarEntity1, ScalarEntity5>();
        var mapper = mapperBuilder.Build();
        var byteArray = new byte[] { 2, 3, 4 };
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            databaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, null, "4", byteArray));
            await databaseContext.SaveChangesAsync();
        });

        // act
        ScalarEntity1? entity = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            entity = await databaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        });

        var instance = mapper.Map<ScalarEntity1, ScalarEntity5>(entity!);

        // assert
        Assert.Equal(2, instance.IntProp);
        Assert.Equal(0, instance.LongNullableProp);
        Assert.Equal("4", instance.StringProp);
        Assert.Equal(byteArray, instance.ByteArrayProp);

        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        ScalarEntity1? result = default;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            result = await mapper.MapAsync<ScalarEntity5, ScalarEntity1>(instance, databaseContext);
        });

        // assert
        Assert.Equal(1, result!.IntProp);
        Assert.Equal(2, result.LongNullableProp);
        Assert.Equal("3", result.StringProp);
        Assert.Equal(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Fact]
    public void ConvertWithDuplicatedScalarMapper_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // assert
        Assert.Throws<ScalarMapperExistsException>(() => mapperBuilder
            .WithScalarConverter<ByteArrayWrapper, byte[]>((wrapper) => ByteArrayWrapper.ConvertStatic(wrapper!)!)
            .WithScalarConverter<ByteArrayWrapper, byte[]>((wrapper) => wrapper!.Bytes!, true));
    }

    [Fact]
    public void RegisterTarget_WithoutDefaultConstructorAndNoFactoryMethod_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act & assert
        Assert.Throws<FactoryMethodException>(() => mapperBuilder.Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Fact]
    public void RegisterTarget_WithDefaultConstructorAndFactoryMethod_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act & assert
        Assert.Throws<FactoryMethodException>(() => mapperBuilder
            .WithFactoryMethod<ScalarEntity1>(() => new ScalarEntity1())
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Fact]
    public void RegisterTarget_WithDuplicatedFactoryMethod_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act & assert
        Assert.Throws<FactoryMethodExistsException>(() => mapperBuilder
            .WithFactoryMethod<EntityWithoutDefaultConstructor>(() => new EntityWithoutDefaultConstructor(1))
            .WithFactoryMethod<EntityWithoutDefaultConstructor>(() => new EntityWithoutDefaultConstructor(2), true)
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Fact]
    public void RegisterTarget_WithDuplicatedConfig_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act & assert
        Assert.Throws<TypeConfiguratedException>(() => mapperBuilder
            .WithConfiguration<EntityWithoutDefaultConstructor>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.Timestamp)))
            .WithConfiguration<EntityWithoutDefaultConstructor>(new TypeConfiguration(nameof(EntityBase.Id), nameof(EntityBase.Timestamp)), true)
            .Register<ScalarEntity1, EntityWithoutDefaultConstructor>());
    }

    [Fact]
    public void FactoryMethodForScalarType_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidEntityTypeException>(() =>
        {
            mapperBuilder
                .WithScalarConverter<EntityWithoutDefaultConstructor, int>(e => 1)
                .WithFactoryMethod<EntityWithoutDefaultConstructor>(() => new EntityWithoutDefaultConstructor(100));
        });
    }

    [Fact]
    public void ScalarTypeForFactoryMethodType_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidScalarTypeException>(() =>
        {
            mapperBuilder
                .WithFactoryMethod<EntityWithoutDefaultConstructor>(() => new EntityWithoutDefaultConstructor(100))
                .WithScalarConverter<EntityWithoutDefaultConstructor, int>(e => 1);
        });
    }

    [Fact]
    public void ConfigurationForScalarType_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidEntityTypeException>(() =>
        {
            mapperBuilder
                .WithScalarConverter<ScalarEntityCustomKeyProperties1, int>(s => 1)
                .WithConfiguration<ScalarEntityCustomKeyProperties1>(new TypeConfiguration(nameof(EntityBase.Timestamp), nameof(EntityBase.Id)));
        });
    }

    [Fact]
    public void ScalarTypeForConfiguration_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, DefaultConfiguration);

        // act and assert
        Assert.Throws<InvalidScalarTypeException>(() =>
        {
            mapperBuilder
                .WithConfiguration<ScalarEntityCustomKeyProperties1>(new TypeConfiguration(nameof(EntityBase.Timestamp), nameof(EntityBase.Id)))
                .WithScalarConverter<ScalarEntityCustomKeyProperties1, int>(s => 1);
        });
    }
}

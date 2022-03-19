namespace Oasis.EntityFrameworkCore.Mapper.Test.Scalar;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public sealed class ScalarPropertyMappingTests : TestBase
{
    [Fact]
    public async Task MapScalarProperties_ValidProperties_ShouldBeMapped()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.RegisterTwoWay<ScalarEntity1, ScalarEntity2>();
        var mapper = mapperBuilder.Build();

        DatabaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(2, 3, "4", new byte[] { 2, 3, 4 }));
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<ScalarEntity1>().AsNoTracking().SingleAsync();
        var session1 = mapper.CreateMappingSession();
        var instance = session1.Map<ScalarEntity1, ScalarEntity2>(entity);
        instance.IntProp = 1;
        instance.LongNullableProp = 2;
        instance.StringProp = "3";
        instance.ByteArrayProp = new byte[] { 1, 2, 3 };
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result = await session2.MapAsync<ScalarEntity2, ScalarEntity1>(instance);

        // assert
        Assert.Equal(1, result.IntProp);
        Assert.Equal(2, result.LongNullableProp);
        Assert.Equal("3", result.StringProp);
        Assert.Equal(result.ByteArrayProp, instance.ByteArrayProp);
    }

    [Fact]
    public async Task MapScalarProperties_InvalidProperties_ShouldNotBeMapped()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity3>();
        var mapper = mapperBuilder.Build();

        DatabaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<ScalarEntity1>().AsNoTracking().SingleAsync();
        var session = mapper.CreateMappingSession();
        var result = session.Map<ScalarEntity1, ScalarEntity3>(entity);

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
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder.Register<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        DatabaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, 2, "3", new byte[] { 1, 2, 3 }));
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<ScalarEntity1>().FirstAsync();
        var session = mapper.CreateMappingSession();
        var result = session.Map<ScalarEntity1, ScalarEntity4>(entity);

        // assert
        Assert.Null(result.ByteArrayProp);
    }

    [Fact]
    public async Task ConvertWithLambdaExpressionScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper((ByteArrayWrapper wrapper) => wrapper.Bytes)
            .WithScalarMapper((byte[] array) => new ByteArrayWrapper(array))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        DatabaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, null, "abc", new byte[] { 1, 2, 3 }));
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result2 = await session2.MapAsync<ScalarEntity4, ScalarEntity1>(result1);

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes, result2.ByteArrayProp!));
    }

    [Fact]
    public async Task ConvertWithStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .WithScalarMapper((ByteArrayWrapper wrapper) => ByteArrayWrapper.ConvertStatic(wrapper))
            .WithScalarMapper((byte[] array) => ByteArrayWrapper.ConvertStatic(array))
            .RegisterTwoWay<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        DatabaseContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1, null, "abc", new byte[] { 1, 2, 3 }));
        await DatabaseContext.SaveChangesAsync();

        // act
        var entity = await DatabaseContext.Set<ScalarEntity1>().AsNoTracking().FirstAsync();
        var session1 = mapper.CreateMappingSession();
        var result1 = session1.Map<ScalarEntity1, ScalarEntity4>(entity);
        result1.ByteArrayProp = new ByteArrayWrapper(new byte[] { 2, 3, 4 });
        var session2 = mapper.CreateMappingToDatabaseSession(DatabaseContext);
        var result2 = await session2.MapAsync<ScalarEntity4, ScalarEntity1>(result1);

        // assert
        Assert.True(Enumerable.SequenceEqual(result1.ByteArrayProp!.Bytes, result2.ByteArrayProp!));
    }

    [Fact]
    public void ConvertWithDuplicatedScalarMapper_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // assert
        Assert.Throws<ScalarMapperExistsException>(() => mapperBuilder
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => ByteArrayWrapper.ConvertStatic(wrapper))
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => wrapper.Bytes));
    }
}

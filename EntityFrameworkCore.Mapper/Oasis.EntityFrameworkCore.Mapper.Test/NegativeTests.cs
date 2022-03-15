namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class NegativeTests : IDisposable
{
    private readonly DbContext _dbContext;

    public NegativeTests()
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseInMemoryDatabase(GetType().Name)
            .Options;
        _dbContext = new DatabaseContext(options);
        _dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task MapListProperties_UpdateNonExistingNavitation_ShouldFail()
    {
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var instance = new ListIEntity1(1, 2, new List<SubScalarEntity1> { new SubScalarEntity1(2, 2, 3, "4", new byte[] { 2 }) });
        var sub = new SubScalarEntity1(1, 1, 2, "3", new byte[] { 1 });
        sub.ListIEntityId = 1;
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, 1, new List<SubScalarEntity1>()));
        _dbContext.Set<SubScalarEntity1>().Add(sub);
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            var session = mapper.CreateMappingToEntitiesSession(_dbContext);
            await session.MapAsync<ListIEntity1, CollectionEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });
    }

    [Fact]
    public void ConvertWithDuplicatedScalarMapper_ShouldFail()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        Assert.Throws<ScalarMapperExistsException>(() => mapperBuilder
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => ByteArrayWrapper.ConvertStatic(wrapper))
            .WithScalarMapper<ByteArrayWrapper, byte[]>((wrapper) => wrapper.Bytes));
    }

    [Fact]
    public async Task ConvertWithoutStaticScalarMapper_ShouldSucceed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        mapperBuilder
            .Register<ScalarEntity4, ScalarEntity1>()
            .Register<ScalarEntity1, ScalarEntity4>();

        var mapper = mapperBuilder.Build();

        var instance = new ScalarEntity4(1, new byte[] { 1, 2, 3 });
        _dbContext.Set<ScalarEntity1>().Add(new ScalarEntity1(1));
        await _dbContext.SaveChangesAsync();

        // act
        var session = mapper.CreateMappingToEntitiesSession(_dbContext);
        var result = await session.MapAsync<ScalarEntity4, ScalarEntity1>(instance, x => x.AsNoTracking());

        // assert
        Assert.Null(result.ByteArrayProp);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}

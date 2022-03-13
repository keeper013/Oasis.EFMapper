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

        var instance = new ListIEntity1(1, 2, new List<ScalarEntity2> { new ScalarEntity2(2, 2, 3, "4", new byte[] { 2 }) });
        _dbContext.Set<CollectionEntity1>().Add(new CollectionEntity1(1, 1, new List<ScalarEntity1> { new ScalarEntity1(1, 1, 2, "3", new byte[] { 1 }) }));
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<EntityNotFoundException>(async () =>
        {
            var session = mapper.CreateMappingToEntitiesSession(_dbContext);
            await session.Map<ListIEntity1, CollectionEntity1>(instance, x => x.AsNoTracking().Include(x => x.Scs));
        });
    }

    [Fact]
    public void ConvertWithScalarMapper_InstanceMethodNotAllowed()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        Assert.Throws<NonStaticScalarMapperException>(() => mapperBuilder.WithScalarMapper<ByteArrayWrapper, byte[]>((ByteArrayWrapper wrapper) => wrapper.Bytes));
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}

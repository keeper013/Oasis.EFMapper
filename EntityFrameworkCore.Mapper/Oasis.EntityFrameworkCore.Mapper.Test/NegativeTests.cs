namespace Oasis.EntityFrameworkCore.Mapper.Test;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System;
using System.Collections.Generic;
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
    public void MapWithoutContext_ShouldFail()
    {
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionEntity1(1, 1, new List<ScalarEntity1> { new ScalarEntity1(1, 1, 2, "3", new byte[] { 1 }) });
        var lic1 = new ListIEntity1(1, 2, new List<ScalarEntity2> { new ScalarEntity2(2, 2, 3, "4", new byte[] { 2 }) });

        Assert.Throws<MappingContextNotStartedException>(() => mapper.Map(lic1, cc1));
    }

    [Fact]
    public void StartingContextWithoutDisposingExisting_ShouldFail()
    {
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionEntity1(1, 1, new List<ScalarEntity1> { new ScalarEntity1(1, 1, 2, "3", new byte[] { 1 }) });
        var lic1 = new ListIEntity1(1, 2, new List<ScalarEntity2> { new ScalarEntity2(2, 2, 3, "4", new byte[] { 2 }) });

        Assert.Throws<MappingContextStartedException>(() =>
        {
            using var context1 = mapper.StartMappingContext(_dbContext);
            using var context2 = mapper.StartMappingContext(_dbContext);
            mapper.Map(lic1, cc1);
        });
    }

    [Fact]
    public void MapListProperties_UpdateNonExistingNavitation_ShouldFail()
    {
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        mapperBuilder.Register<ListIEntity1, CollectionEntity1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionEntity1(1, 1, new List<ScalarEntity1> { new ScalarEntity1(1, 1, 2, "3", new byte[] { 1 }) });
        var lic1 = new ListIEntity1(1, 2, new List<ScalarEntity2> { new ScalarEntity2(2, 2, 3, "4", new byte[] { 2 }) });

        Assert.Throws<EntityNotFoundException>(() =>
        {
            using (var context = mapper.StartMappingContext(_dbContext))
            {
                mapper.Map(lic1, cc1);
            }
        });
    }

    [Fact]
    public void ConvertWithScalarMapper_InstanceMethodNotAllowed()
    {
        // arrange
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);
        Assert.Throws<NonStaticScalarMapperException>(() => mapperBuilder.WithScalarMapper<ByteArrayWrapper, byte[]>((ByteArrayWrapper wrapper) => wrapper.Bytes));
    }

    public void Dispose() => _dbContext.Dispose();
}

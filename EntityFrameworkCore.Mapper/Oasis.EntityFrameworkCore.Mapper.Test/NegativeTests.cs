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
    public void MapListProperties_UpdateNonExistingNavitation_ShouldFail()
    {
        var factory = new MapperFactory();
        var mapperBuilder = factory.Make(GetType().Name);

        // TODO: shouldn't need to call this when cascade register is implemented
        mapperBuilder.Register<ScalarClass2, ScalarClass1>();
        mapperBuilder.Register<ListIClass1, CollectionClass1>();

        var mapper = mapperBuilder.Build();

        var cc1 = new CollectionClass1(1, 1, new List<ScalarClass1> { new ScalarClass1(1, 1, 2, "3", new byte[] { 1 }) });
        var lic1 = new ListIClass1(1, 2, new List<ScalarClass2> { new ScalarClass2(2, 2, 3, "4", new byte[] { 2 }) });

        Assert.Throws<EntityNotFoundException>(() => mapper.Map(lic1, cc1, _dbContext));
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

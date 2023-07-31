namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepEntityOnMappingRemoved;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public sealed class MappingRemovedTests : TestBase
{
    [Fact]
    public void PropertyConfig_WrongPropertyName_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
            .SetMappingKeepEntityOnMappingRemoved(false)
            .PropertyKeepEntityOnMappingRemoved("NoneExistingPropertyName", true)
            .Build();
        var mapperBuilder = factory.MakeMapperBuilder(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, false);
        var mapper = mapperBuilder
            .WithConfiguration<MappingRemovedDependant1>(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), null, false)
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(config);

        // act and assert
        Assert.Throws<CustomTypePropertyEntityRemoverException>(() => mapper.Build());
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task DefaultConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, defaultKeep);
        var mapper = mapperBuilder.RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task DefaultConfig_RemoveListItm_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, defaultKeep);
        var mapper = mapperBuilder.RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task TypeConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder.WithConfiguration<MappingRemovedDependant1>(null, null, null, defaultKeep).RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task TypeConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder.WithConfiguration<MappingRemovedDependant1>(null, null, null, defaultKeep).RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task MappingConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().SetMappingKeepEntityOnMappingRemoved(defaultKeep).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder
            .WithConfiguration<MappingRemovedDependant1>(null, null, null, !defaultKeep)
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(config).Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task MappingConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<MappingRemovedPrincipal2, MappingRemovedPrincipal1>().SetMappingKeepEntityOnMappingRemoved(defaultKeep).Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder
            .WithConfiguration<MappingRemovedDependant1>(null, null, null, !defaultKeep)
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(config).Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task PropertyConfig_RemoveEntity_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
            .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
            .PropertyKeepEntityOnMappingRemoved(nameof(MappingRemovedPrincipal1.OptionalDependant), defaultKeep)
            .Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder
            .WithConfiguration<MappingRemovedDependant1>(null, null, null, !defaultKeep)
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(config).Build();

        // act and assert
        await RemoveEntityTest(mapper, dependantCount);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public async Task PropertyConfig_RemoveListItem_Test(bool defaultKeep, int dependantCount)
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<MappingRemovedPrincipal2, MappingRemovedPrincipal1>()
            .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
            .PropertyKeepEntityOnMappingRemoved(nameof(MappingRemovedPrincipal1.DependantList), defaultKeep)
            .Build();
        var mapperBuilder = MakeDefaultMapperBuilder(factory, !defaultKeep);
        var mapper = mapperBuilder
            .WithConfiguration<MappingRemovedDependant1>(null, null, null, !defaultKeep)
            .RegisterTwoWay<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(config).Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    public async Task RemoveEntityTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new MappingRemovedPrincipal2 { OptionalDependant = new MappingRemovedDependant2 { IntProp = 1 } };
            var mappedPrincipal = await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });

        MappingRemovedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<MappingRemovedPrincipal1>().Include(p => p.OptionalDependant).FirstAsync();
            mappedPrincipal = mapper.Map<MappingRemovedPrincipal1, MappingRemovedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.OptionalDependant = null;
            await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(mappedPrincipal, p => p.Include(p => p.OptionalDependant), databaseContext, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.Equal(dependantCount, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });
    }

    public async Task RemoveListItemTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new MappingRemovedPrincipal2 { DependantList = new List<MappingRemovedDependant2> { new MappingRemovedDependant2 { IntProp = 1 } } };
            var mappedPrincipal = await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(principal, null, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });

        MappingRemovedPrincipal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<MappingRemovedPrincipal1>().Include(p => p.DependantList).FirstAsync();
            mappedPrincipal = mapper.Map<MappingRemovedPrincipal1, MappingRemovedPrincipal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependantList.Clear();
            await mapper.MapAsync<MappingRemovedPrincipal2, MappingRemovedPrincipal1>(mappedPrincipal, p => p.Include(p => p.DependantList), databaseContext, MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<MappingRemovedPrincipal1>().CountAsync());
            Assert.Equal(dependantCount, await databaseContext.Set<MappingRemovedDependant1>().CountAsync());
        });
    }
}

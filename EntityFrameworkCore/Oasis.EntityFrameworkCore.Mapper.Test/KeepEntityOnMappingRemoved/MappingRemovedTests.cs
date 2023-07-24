namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepEntityOnMappingRemoved;

using Microsoft.EntityFrameworkCore;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class MappingRemovedTests : TestBase
{
    [Fact]
    public void PropertyConfig_WrongPropertyName_ShouldThrowException()
    {
        // arrange
        var factory = new MapperBuilderFactory();
        var config = factory.MakeCustomTypeMapperBuilder<Principal2, Principal1>()
            .SetMappingKeepEntityOnMappingRemoved(false)
            .PropertyKeepEntityOnMappingRemoved("NoneExistingPropertyName", true)
            .Build();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(false));
        var mapper = mapperBuilder
            .WithConfiguration<Dependant1>(BuildEntityConfiguration(false))
            .RegisterTwoWay<Principal2, Principal1>(config);

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
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(defaultKeep));
        var mapper = mapperBuilder.RegisterTwoWay<Principal2, Principal1>().Build();

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
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(defaultKeep));
        var mapper = mapperBuilder.RegisterTwoWay<Principal2, Principal1>().Build();

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
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder.WithConfiguration<Dependant1>(BuildEntityConfiguration(defaultKeep)).RegisterTwoWay<Principal2, Principal1>().Build();

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
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder.WithConfiguration<Dependant1>(BuildEntityConfiguration(defaultKeep)).RegisterTwoWay<Principal2, Principal1>().Build();

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
        var config = factory.MakeCustomTypeMapperBuilder<Principal2, Principal1>().SetMappingKeepEntityOnMappingRemoved(defaultKeep).Build();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder
            .WithConfiguration<Dependant1>(BuildEntityConfiguration(!defaultKeep))
            .RegisterTwoWay<Principal2, Principal1>(config).Build();

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
        var config = factory.MakeCustomTypeMapperBuilder<Principal2, Principal1>().SetMappingKeepEntityOnMappingRemoved(defaultKeep).Build();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder
            .WithConfiguration<Dependant1>(BuildEntityConfiguration(!defaultKeep))
            .RegisterTwoWay<Principal2, Principal1>(config).Build();

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
        var config = factory.MakeCustomTypeMapperBuilder<Principal2, Principal1>()
            .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
            .PropertyKeepEntityOnMappingRemoved(nameof(Principal1.OptionalDependant), defaultKeep)
            .Build();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder
            .WithConfiguration<Dependant1>(BuildEntityConfiguration(!defaultKeep))
            .RegisterTwoWay<Principal2, Principal1>(config).Build();

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
        var config = factory.MakeCustomTypeMapperBuilder<Principal2, Principal1>()
            .SetMappingKeepEntityOnMappingRemoved(!defaultKeep)
            .PropertyKeepEntityOnMappingRemoved(nameof(Principal1.DependentList), defaultKeep)
            .Build();
        var mapperBuilder = factory.MakeMapperBuilder(GetType().Name, BuildEntityConfiguration(!defaultKeep));
        var mapper = mapperBuilder
            .WithConfiguration<Dependant1>(BuildEntityConfiguration(!defaultKeep))
            .RegisterTwoWay<Principal2, Principal1>(config).Build();

        // act and assert
        await RemoveListItemTest(mapper, dependantCount);
    }

    public async Task RemoveEntityTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new Principal2 { OptionalDependant = new Dependant2 { IntProp = 1 } };
            var mappedPrincipal = await mapper.MapAsync<Principal2, Principal1>(principal, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<Principal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<Dependant1>().CountAsync());
        });

        Principal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<Principal1>().Include(p => p.OptionalDependant).FirstAsync();
            mappedPrincipal = mapper.Map<Principal1, Principal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.OptionalDependant = null;
            await mapper.MapAsync<Principal2, Principal1>(mappedPrincipal, databaseContext, p => p.Include(p => p.OptionalDependant), MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<Principal1>().CountAsync());
            Assert.Equal(dependantCount, await databaseContext.Set<Dependant1>().CountAsync());
        });
    }

    public async Task RemoveListItemTest(IMapper mapper, int dependantCount)
    {
        // act
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = new Principal2 { DependentList = new List<Dependant2> { new Dependant2 { IntProp = 1 } } };
            var mappedPrincipal = await mapper.MapAsync<Principal2, Principal1>(principal, databaseContext);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<Principal1>().CountAsync());
            Assert.Equal(1, await databaseContext.Set<Dependant1>().CountAsync());
        });

        Principal2 mappedPrincipal = null!;
        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            var principal = await databaseContext.Set<Principal1>().Include(p => p.DependentList).FirstAsync();
            mappedPrincipal = mapper.Map<Principal1, Principal2>(principal);
        });

        await ExecuteWithNewDatabaseContext(async (databaseContext) =>
        {
            mappedPrincipal.DependentList.Clear();
            await mapper.MapAsync<Principal2, Principal1>(mappedPrincipal, databaseContext, p => p.Include(p => p.DependentList), MapToDatabaseType.Update);
            await databaseContext.SaveChangesAsync();
            Assert.Equal(1, await databaseContext.Set<Principal1>().CountAsync());
            Assert.Equal(dependantCount, await databaseContext.Set<Dependant1>().CountAsync());
        });
    }

    private EntityConfiguration BuildEntityConfiguration(bool keep)
    {
        return new EntityConfiguration(nameof(EntityBase.Id), nameof(EntityBase.ConcurrencyToken), keep);
    }
}

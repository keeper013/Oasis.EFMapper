namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal sealed class MapperBuilderConfigurationBuilder : IMapperBuilderConfigurationBuilder, IMapperBuilderConfiguration
{
    private readonly HashSet<string> _excludedProperties = new ();
    private readonly IMapperBuilderFactory _factory;

    public MapperBuilderConfigurationBuilder(IMapperBuilderFactory factory)
    {
        MapType = MapType.MemoryAndUpsert;
        _factory = factory;
    }

    public string? IdentityPropertyName { get; private set; }

    public string? ConcurrencyTokenPropertyName { get; private set; }

    public IReadOnlySet<string>? ExcludedProperties => _excludedProperties.Any() ? _excludedProperties : default;

    public MapType MapType { get; private set; }

    public bool ThrowForRedundantConfiguration { get; private set; }

    public IMapperBuilderConfigurationBuilder ExcludedPropertiesByName(params string[]? names)
    {
        if (names != null && names.Any())
        {
            _excludedProperties.UnionWith(names);
        }

        return this;
    }

    public IMapperBuilderFactory Finish()
    {
        return _factory;
    }

    public IMapperBuilderConfigurationBuilder SetConcurrencyTokenPropertyName(string? concurrencyTokenPropertyName)
    {
        ConcurrencyTokenPropertyName = concurrencyTokenPropertyName;
        return this;
    }

    public IMapperBuilderConfigurationBuilder SetIdentityPropertyName(string? identityPropertyName)
    {
        IdentityPropertyName = identityPropertyName;
        return this;
    }

    public IMapperBuilderConfigurationBuilder SetKeyPropertyNames(string? identityPropertyName, string? concurrencyTokenPropertyName = null)
    {
        IdentityPropertyName = identityPropertyName;
        ConcurrencyTokenPropertyName = concurrencyTokenPropertyName;
        return this;
    }

    public IMapperBuilderConfigurationBuilder SetMapType(MapType? mapToDatabase)
    {
        MapType = mapToDatabase ?? MapType.MemoryAndUpsert;
        return this;
    }

    public IMapperBuilderConfigurationBuilder SetThrowForRedundantConfiguration(bool? doThrow)
    {
        ThrowForRedundantConfiguration = doThrow ?? true;
        return this;
    }
}

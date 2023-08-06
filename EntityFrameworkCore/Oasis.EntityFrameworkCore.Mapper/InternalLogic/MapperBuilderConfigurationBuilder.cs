namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal sealed class MapperBuilderConfigurationBuilder : BuilderConfiguration<MapperBuilderFactory, IMapperBuilderFactory>, IMapperBuilderConfigurationBuilder, IMapperBuilderConfiguration
{
    private readonly HashSet<string> _excludedProperties = new ();

    public MapperBuilderConfigurationBuilder(MapperBuilderFactory factory)
        : base(factory)
    {
        MapToDatabaseType = MapToDatabaseType.Upsert;
    }

    public string? IdentityPropertyName { get; private set; }

    public string? ConcurrencyTokenPropertyName { get; private set; }

    public IReadOnlySet<string>? ExcludedProperties => _excludedProperties.Any() ? _excludedProperties : default;

    public MapToDatabaseType MapToDatabaseType { get; private set; }

    public bool ThrowForRedundantConfiguration { get; private set; }

    public IMapperBuilderConfigurationBuilder ExcludedPropertiesByName(params string[]? names)
    {
        if (names != null && names.Any())
        {
            _excludedProperties.UnionWith(names);
        }

        return this;
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

    public IMapperBuilderConfigurationBuilder SetMapToDatabaseType(MapToDatabaseType? mapToDatabase)
    {
        MapToDatabaseType = mapToDatabase ?? MapToDatabaseType.Upsert;
        return this;
    }

    public IMapperBuilderConfigurationBuilder SetThrowForRedundantConfiguration(bool? doThrow)
    {
        ThrowForRedundantConfiguration = doThrow ?? true;
        return this;
    }

    protected override void Configure(MapperBuilderFactory configurator)
    {
        configurator.Configure(this);
    }
}

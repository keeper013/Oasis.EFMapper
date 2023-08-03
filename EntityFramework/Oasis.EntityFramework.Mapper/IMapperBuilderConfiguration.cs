namespace Oasis.EntityFramework.Mapper;

public interface IMapperBuilderConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    ISet<string>? ExcludedProperties { get; }

    bool KeepEntityOnMappingRemoved { get; }

    MapToDatabaseType MapToDatabaseType { get; }

    bool ThrowForRedundantConfiguration { get; }
}

public interface IMapperBuilderConfigurationBuilder : IConfigurator<IMapperBuilderFactory>
{
    IMapperBuilderConfigurationBuilder SetIdentityPropertyName(string? identityPropertyName);

    IMapperBuilderConfigurationBuilder SetConcurrencyTokenPropertyName(string concurrencyTokenPropertyName);

    IMapperBuilderConfigurationBuilder SetKeyPropertyNames(string? identityPropertyName, string? concurrencyTokenPropertyName = null);

    IMapperBuilderConfigurationBuilder ExcludedPropertiesByName(params string[]? names);

    IMapperBuilderConfigurationBuilder SetKeepEntityOnMappingRemoved(bool? keepEntityOnMappingRemoved);

    IMapperBuilderConfigurationBuilder SetMapToDatabaseType(MapToDatabaseType? mapToDatabase);

    IMapperBuilderConfigurationBuilder SetThrowForRedundantConfiguration(bool? doThrow);
}
namespace Oasis.EntityFramework.Mapper;

[Flags]
public enum MapToDatabaseType : byte
{
    /// <summary>
    /// Neither insert nor update is allowed
    /// </summary>
    None = 0,

    /// <summary>
    /// Insert
    /// </summary>
    Insert = 1,

    /// <summary>
    /// Update
    /// </summary>
    Update = 2,

    /// <summary>
    /// Insert or Update
    /// </summary>
    Upsert = Insert | Update,
}

public interface IMapperBuilderConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    ISet<string>? ExcludedProperties { get; }

    MapToDatabaseType MapToDatabaseType { get; }

    bool ThrowForRedundantConfiguration { get; }
}

public interface IMapperBuilderConfigurationBuilder : IConfigurator<IMapperBuilderFactory>
{
    IMapperBuilderConfigurationBuilder SetIdentityPropertyName(string? identityPropertyName);

    IMapperBuilderConfigurationBuilder SetConcurrencyTokenPropertyName(string concurrencyTokenPropertyName);

    IMapperBuilderConfigurationBuilder SetKeyPropertyNames(string? identityPropertyName, string? concurrencyTokenPropertyName = null);

    IMapperBuilderConfigurationBuilder ExcludedPropertiesByName(params string[]? names);

    IMapperBuilderConfigurationBuilder SetMapToDatabaseType(MapToDatabaseType? mapToDatabase);

    IMapperBuilderConfigurationBuilder SetThrowForRedundantConfiguration(bool? doThrow);
}
namespace Oasis.EntityFramework.Mapper;

[Flags]
public enum MapType : byte
{
    /// <summary>
    /// Map to database insert
    /// </summary>
    Insert = 1,

    /// <summary>
    /// Map to database update
    /// </summary>
    Update = 2,

    /// <summary>
    /// Map to database insert and update
    /// </summary>
    Upsert = Insert | Update,

    /// <summary>
    /// Map to memory
    /// </summary>
    Memory = 4,

    /// <summary>
    /// Map to memory and insert to database
    /// </summary>
    MemoryAndInsert = Memory | Insert,

    /// <summary>
    /// Map to memory and update to database
    /// </summary>
    MemoryAndUpdate = Memory | Update,

    /// <summary>
    /// Map to memory and insert and update to database
    /// </summary>
    MemoryAndUpsert = Memory | Insert | Update,
}

public interface IMapperBuilderConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    ISet<string>? ExcludedProperties { get; }

    MapType MapType { get; }

    bool ThrowForRedundantConfiguration { get; }
}

public interface IMapperBuilderConfigurationBuilder : IConfigurator<IMapperBuilderFactory>
{
    IMapperBuilderConfigurationBuilder SetIdentityPropertyName(string? identityPropertyName);

    IMapperBuilderConfigurationBuilder SetConcurrencyTokenPropertyName(string concurrencyTokenPropertyName);

    IMapperBuilderConfigurationBuilder SetKeyPropertyNames(string? identityPropertyName, string? concurrencyTokenPropertyName = null);

    IMapperBuilderConfigurationBuilder ExcludedPropertiesByName(params string[]? names);

    IMapperBuilderConfigurationBuilder SetMapType(MapType? mapToDatabase);

    IMapperBuilderConfigurationBuilder SetThrowForRedundantConfiguration(bool? doThrow);
}
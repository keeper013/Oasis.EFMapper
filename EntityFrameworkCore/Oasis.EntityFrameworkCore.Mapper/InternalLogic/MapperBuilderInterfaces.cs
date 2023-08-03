namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface IPropertyEntityRemover
{
    bool? MappingKeepEntityOnMappingRemoved { get; }

    IReadOnlyDictionary<string, bool>? PropertyKeepEntityOnMappingRemoved { get; }
}

internal interface ICustomPropertyMapper
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    Delegate MapProperties { get; }
}

internal interface ICustomTypeMapperConfiguration
{
    ICustomPropertyMapper? CustomPropertyMapper { get; }

    IPropertyEntityRemover? PropertyEntityRemover { get; }

    IReadOnlySet<string>? ExcludedProperties { get; }

    MapToDatabaseType? MapToDatabaseType { get; }
}

internal interface IEntityConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    IReadOnlySet<string>? ExcludedProperties { get; }

    bool? KeepEntityOnMappingRemoved { get; }
}

internal interface IMapperBuilderInternal
{
    void Configure(ICustomTypeMapperConfiguration configuration);

    void Configure(IEntityConfiguration configuration);
}
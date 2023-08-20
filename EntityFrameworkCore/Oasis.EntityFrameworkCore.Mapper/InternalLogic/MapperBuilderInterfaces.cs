namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface ICustomPropertyMapper
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    Delegate MapProperties { get; }
}

internal interface ICustomTypeMapperConfiguration
{
    ICustomPropertyMapper? CustomPropertyMapper { get; }

    IReadOnlySet<string>? ExcludedProperties { get; }

    IReadOnlySet<string>? KeepUnmatchedProperties { get; }

    MapToDatabaseType? MapToDatabaseType { get; }
}

internal interface IEntityConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    IReadOnlySet<string>? ExcludedProperties { get; }

    IReadOnlySet<string>? KeepUnmatchedProperties { get; }
}

internal interface IMapperBuilderInternal
{
    void Configure(ICustomTypeMapperConfiguration configuration);

    void Configure(IEntityConfiguration configuration);
}
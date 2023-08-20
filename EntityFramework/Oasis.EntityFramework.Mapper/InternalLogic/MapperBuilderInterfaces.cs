namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal interface ICustomPropertyMapper
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    Delegate MapProperties { get; }
}

internal interface ICustomTypeMapperConfiguration
{
    ICustomPropertyMapper? CustomPropertyMapper { get; }

    ISet<string>? ExcludedProperties { get; }

    ISet<string>? KeepUnmatchedProperties { get; }

    MapToDatabaseType? MapToDatabaseType { get; }
}

internal interface IEntityConfiguration
{
    string? IdentityPropertyName { get; }

    string? ConcurrencyTokenPropertyName { get; }

    ISet<string>? ExcludedProperties { get; }

    ISet<string>? KeepUnmatchedProperties { get; }
}

internal interface IMapperBuilderInternal
{
    void Configure(ICustomTypeMapperConfiguration configuration);

    void Configure(IEntityConfiguration configuration);
}
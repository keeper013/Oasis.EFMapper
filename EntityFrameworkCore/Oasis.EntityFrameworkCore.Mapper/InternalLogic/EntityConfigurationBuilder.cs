namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class EntityConfigurationBuilder<TEntity> : BuilderConfiguration<MapperBuilder>, IEntityConfiguration<TEntity>, IEntityConfiguration
    where TEntity : class
{
    private readonly HashSet<string> _excludedProperties = new HashSet<string>();

    public EntityConfigurationBuilder(MapperBuilder configurator)
        : base(configurator)
    {
    }

    public string? IdentityPropertyName { get; private set; }

    public string? ConcurrencyTokenPropertyName { get; private set; }

    public IReadOnlySet<string>? ExcludedProperties => _excludedProperties.Any() ? _excludedProperties : default;

    public bool? KeepEntityOnMappingRemoved { get; private set; }

    public IEntityConfiguration<TEntity> ExcludedPropertiesByName(params string[] names)
    {
        if (names == null || !names.Any())
        {
            throw new ArgumentNullException(nameof(names));
        }

        var properties = typeof(TEntity).GetProperties(Utilities.PublicInstance);
        foreach (var propertyName in names)
        {
            if (!properties.Any(p => string.Equals(p.Name, propertyName)))
            {
                throw new UselessExcludeException(typeof(TEntity), propertyName);
            }
        }

        _excludedProperties.UnionWith(names);
        return this;
    }

    public IMapperBuilder Finish()
    {
        return FinishInternal();
    }

    public IEntityConfiguration<TEntity> SetConcurrencyTokenPropertyName(string concurrencyTokenPropertyName)
    {
        ConcurrencyTokenPropertyName = concurrencyTokenPropertyName;
        return this;
    }

    public IEntityConfiguration<TEntity> SetIdentityPropertyName(string identityPropertyName)
    {
        IdentityPropertyName = identityPropertyName;
        return this;
    }

    public IEntityConfiguration<TEntity> SetKeyPropertyNames(string? identityPropertyName, string? concurrencyTokenPropertyName = null)
    {
        IdentityPropertyName = identityPropertyName;
        ConcurrencyTokenPropertyName = concurrencyTokenPropertyName;
        return this;
    }

    public IEntityConfiguration<TEntity> SetKeepEntityOnMappingRemoved(bool keepEntityOnMappingRemoved)
    {
        KeepEntityOnMappingRemoved = keepEntityOnMappingRemoved;
        return this;
    }

    protected override void Configure(MapperBuilder configurator)
    {
        configurator.Configure<TEntity>(this);
    }
}

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class EntityConfigurationBuilder<TEntity> : BuilderConfiguration<MapperBuilder, IMapperBuilder>, IEntityConfiguration<TEntity>, IEntityConfiguration
    where TEntity : class
{
    public EntityConfigurationBuilder(MapperBuilder configurator)
        : base(configurator)
    {
    }

    public string? IdentityPropertyName { get; private set; }

    public string? ConcurrencyTokenPropertyName { get; private set; }

    public IReadOnlySet<string>? ExcludedProperties { get; private set; }

    public IReadOnlySet<string>? KeepUnmatchedProperties { get; private set; }

    public IEntityConfiguration<TEntity> ExcludePropertiesByName(params string[] names)
    {
        if (names != null && names.Any())
        {
            var properties = typeof(TEntity).GetProperties(Utilities.PublicInstance);
            foreach (var propertyName in names)
            {
                if (!properties.Any(p => string.Equals(p.Name, propertyName)))
                {
                    throw new UselessExcludeException(typeof(TEntity), propertyName);
                }
            }

            ExcludedProperties = new HashSet<string>(names);
        }

        return this;
    }

    public IEntityConfiguration<TEntity> KeepUnmatched(params string[] names)
    {
        if (names != null && names.Any())
        {
            var properties = typeof(TEntity).GetProperties(Utilities.PublicInstance);
            foreach (var propertyName in names)
            {
                var property = properties.FirstOrDefault(p => string.Equals(p.Name, propertyName));
                if (property == null || !property.PropertyType.IsListOfEntityType())
                {
                    throw new InvaildEntityListPropertyException(typeof(TEntity), propertyName);
                }
            }

            KeepUnmatchedProperties = new HashSet<string>(names);
        }

        return this;
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

    protected override void Configure(MapperBuilder configurator)
    {
        configurator.Configure<TEntity>(this);
    }
}

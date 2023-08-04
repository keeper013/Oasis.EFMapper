namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;

internal sealed class EntityConfigurationBuilder<TEntity> : BuilderConfiguration<MapperBuilder>, IEntityConfiguration<TEntity>, IEntityConfiguration
    where TEntity : class
{
    public EntityConfigurationBuilder(MapperBuilder configurator)
        : base(configurator)
    {
    }

    public string? IdentityPropertyName { get; private set; }

    public string? ConcurrencyTokenPropertyName { get; private set; }

    public ISet<string>? ExcludedProperties { get; private set; }

    public ISet<string>? DependentProperties { get; private set; }

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

        ExcludedProperties = new HashSet<string>(names);
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

    public IEntityConfiguration<TEntity> SetDependentProperties(params string[] names)
    {
        if (names == null || !names.Any())
        {
            throw new ArgumentNullException(nameof(names));
        }

        var properties = typeof(TEntity).GetProperties(Utilities.PublicInstance);
        foreach (var propertyName in names)
        {
            var property = properties.FirstOrDefault(p => string.Equals(p.Name, propertyName));
            if (property == null)
            {
                throw new InvalidDependentException(typeof(TEntity), propertyName);
            }

            var propertyType = property.PropertyType;
            if (!propertyType.IsEntityType() && !propertyType.IsListOfEntityType())
            {
                throw new InvalidDependentException(typeof(TEntity), propertyName);
            }
        }

        DependentProperties = new HashSet<string>(names);
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

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private readonly MapperRegistry _mapperRegistry;
    private readonly bool _throwForRedundantConfiguration;

    public MapperBuilder(string assemblyName, IMapperBuilderConfiguration? configuration)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new (module, configuration);
        _throwForRedundantConfiguration = configuration?.ThrowForRedundantConfiguration ?? true;
    }

    public IMapper Build()
    {
        var type = _mapperRegistry.Build();
        var scalarTypeConverter = _mapperRegistry.MakeScalarTypeConverter();
        var listTypeConstructor = _mapperRegistry.MakeListTypeConstructor(type);
        var lookup = _mapperRegistry.MakeMapperSetLookUp(type);
        var proxy = _mapperRegistry.MakeEntityBaseProxy(type, scalarTypeConverter);
        var entityFactory = _mapperRegistry.MakeEntityFactory(type);
        var newTargetTrackerProvider = _mapperRegistry.MakeNewTargetTrackerProvider(entityFactory);
        var dependentPropertyManager = _mapperRegistry.MakeDependentPropertyManager();
        var mapToDatabaseTypeManager = _mapperRegistry.MakeMapToDatabaseTypeManager();
        var existingTargetTrackerFactory = _mapperRegistry.MakeExistingTargetTrackerFactory(type);

        // release some memory ahead
        _mapperRegistry.Clear();

        return new Mapper(scalarTypeConverter, listTypeConstructor, lookup, existingTargetTrackerFactory, proxy, newTargetTrackerProvider, dependentPropertyManager, mapToDatabaseTypeManager, entityFactory);
    }

    public IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        _mapperRegistry.Register(typeof(TSource), typeof(TTarget));
        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        _mapperRegistry.Register(sourceType, targetType);
        if (sourceType != targetType)
        {
            _mapperRegistry.Register(targetType, sourceType);
        }

        return this;
    }

    IMapperBuilder IMapperBuilder.WithFactoryMethod<TList, TItem>(Expression<Func<TList>> factoryMethod, bool throwIfRedundant)
    {
        _mapperRegistry.WithFactoryMethod(typeof(TList), typeof(TItem), factoryMethod.Compile(), throwIfRedundant);
        return this;
    }

    public IMapperBuilder WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class
    {
        _mapperRegistry.WithFactoryMethod(typeof(TEntity), factoryMethod.Compile(), throwIfRedundant);
        return this;
    }

    public IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression, bool throwIfRedundant = false)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (sourceType == targetType)
        {
            throw new SameTypeException(targetType);
        }

        _mapperRegistry.WithScalarConverter(typeof(TSource), typeof(TTarget), expression.Compile(), throwIfRedundant);
        return this;
    }

    public IEntityConfiguration<TEntity> Configure<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_throwForRedundantConfiguration && _mapperRegistry.IsConfigured(type))
        {
            throw new RedundantConfiguratedException(type);
        }

        return new EntityConfigurationBuilder<TEntity>(this);
    }

    public ICustomTypeMapperConfiguration<TSource, TTarget> Configure<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (_throwForRedundantConfiguration && _mapperRegistry.IsConfigured(sourceType, targetType))
        {
            throw new RedundantConfiguratedException(sourceType, targetType);
        }

        return new CustomTypeMapperBuilder<TSource, TTarget>(this);
    }

    internal void Configure<TEntity>(IEntityConfiguration configuration)
    {
        if (configuration.IdentityPropertyName == null && configuration.ConcurrencyTokenPropertyName == null && configuration.ExcludedProperties == null
                && configuration.DependentProperties == null)
        {
            throw new EmptyConfiguratedException(typeof(IEntityConfiguration));
        }

        _mapperRegistry.Configure(typeof(TEntity), configuration);
    }

    internal void Configure<TSource, TTarget>(ICustomTypeMapperConfiguration configuration)
    {
        if (configuration.CustomPropertyMapper == null && configuration.ExcludedProperties == null && configuration.MapToDatabaseType == null)
        {
            throw new EmptyConfiguratedException(typeof(ICustomTypeMapperConfiguration));
        }

        if (configuration.ExcludedProperties != null)
        {
            if (configuration.CustomPropertyMapper != null)
            {
                var excluded = configuration.CustomPropertyMapper.MappedTargetProperties.FirstOrDefault(p => configuration.ExcludedProperties.Contains(p.Name));
                if (excluded != null)
                {
                    throw new CustomMappingPropertyExcludedException(typeof(TSource), typeof(TTarget), excluded.Name);
                }
            }
        }

        _mapperRegistry.Configure(typeof(TSource), typeof(TTarget), configuration);
    }
}

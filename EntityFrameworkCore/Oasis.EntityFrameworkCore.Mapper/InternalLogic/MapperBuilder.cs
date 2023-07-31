namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private readonly MapperRegistry _mapperRegistry;

    public MapperBuilder(
        string assemblyName,
        string? identityPropertyName = default,
        string? concurrencyTokenPropertyName = default,
        IReadOnlySet<string>? excludedProperties = default,
        bool? keepEntityOnMappingRemoved = default,
        MapToDatabaseType? mapToDatabase = default)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new (module, identityPropertyName, concurrencyTokenPropertyName, excludedProperties, keepEntityOnMappingRemoved, mapToDatabase);
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
        var entityRemover = _mapperRegistry.MakeEntityRemover();
        var mapToDatabaseTypeManager = _mapperRegistry.MakeMapToDatabaseTypeManager();
        var existingTargetTrackerFactory = _mapperRegistry.MakeExistingTargetTrackerFactory(type);

        // release some memory ahead
        _mapperRegistry.Clear();

        return new Mapper(scalarTypeConverter, listTypeConstructor, lookup, existingTargetTrackerFactory, proxy, newTargetTrackerProvider, entityRemover, mapToDatabaseTypeManager, entityFactory);
    }

    public IMapperBuilder Register<TSource, TTarget>(ICustomTypeMapperConfiguration<TSource, TTarget>? configuration = null)
        where TSource : class
        where TTarget : class
    {
        _mapperRegistry.Register(configuration);
        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>(
        ICustomTypeMapperConfiguration<TSource, TTarget>? sourceToTargetConfiguration = null,
        ICustomTypeMapperConfiguration<TTarget, TSource>? targetToSourceConfiguration = null)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (sourceType == targetType)
        {
            _mapperRegistry.Register(sourceToTargetConfiguration);
        }

        _mapperRegistry.Register(sourceToTargetConfiguration);
        _mapperRegistry.Register(targetToSourceConfiguration);

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

    public IMapperBuilder WithConfiguration<TEntity>(
        string? identityPropertyName,
        string? concurrencyTokenPropertyName = default,
        string[]? excludedProperties = default,
        bool? keepEntityOnMappingRemoved = default,
        bool throwIfRedundant = false)
        where TEntity : class
    {
        var excludedProps = excludedProperties != null && excludedProperties.Any() ? new HashSet<string>(excludedProperties) : null;
        _mapperRegistry.WithConfiguration(
            typeof(TEntity),
            identityPropertyName,
            concurrencyTokenPropertyName,
            excludedProps,
            keepEntityOnMappingRemoved,
            throwIfRedundant);
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
}

namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    internal const bool DefaultKeepEntityOnMappingRemoved = true;
    private readonly MapperRegistry _mapperRegistry;

    public MapperBuilder(string assemblyName, EntityConfiguration defaultConfiguration)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFramework.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new (module, defaultConfiguration);
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
        var existingTargetTrackerFactory = _mapperRegistry.MakeExistingTargetTrackerFactory(type);

        // release some memory ahead
        _mapperRegistry.Clear();

        return new Mapper(scalarTypeConverter, listTypeConstructor, lookup, existingTargetTrackerFactory, proxy, newTargetTrackerProvider, entityRemover, entityFactory);
    }

    public IMapperBuilder Register<TSource, TTarget>(ICustomTypeMapperConfiguration? configuration = null)
        where TSource : class
        where TTarget : class
    {
        _mapperRegistry.Register(typeof(TSource), typeof(TTarget), configuration);
        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>(
        ICustomTypeMapperConfiguration? sourceToTargetConfiguration = null,
        ICustomTypeMapperConfiguration? targetToSourceConfiguration = null)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (sourceType == targetType)
        {
            _mapperRegistry.Register(sourceType, targetType, sourceToTargetConfiguration);
        }

        _mapperRegistry.Register(sourceType, targetType, sourceToTargetConfiguration);
        _mapperRegistry.Register(targetType, sourceType, targetToSourceConfiguration);

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

    public IMapperBuilder WithConfiguration<TEntity>(EntityConfiguration configuration, bool throwIfRedundant = false)
        where TEntity : class
    {
        _mapperRegistry.WithConfiguration(typeof(TEntity), configuration, throwIfRedundant);
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

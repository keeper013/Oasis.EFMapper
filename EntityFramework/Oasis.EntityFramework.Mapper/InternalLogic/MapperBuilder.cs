namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly MapperRegistry _mapperRegistry;

    public MapperBuilder(string assemblyName, TypeConfiguration defaultConfiguration)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new (defaultConfiguration);
        _dynamicMethodBuilder = new DynamicMethodBuilder(
            module.DefineType("Mapper", TypeAttributes.Public),
            _mapperRegistry.ScalarMapperTypeValidator,
            _mapperRegistry.EntityMapperTypeValidator,
            _mapperRegistry.EntityListMapperTypeValidator,
            _mapperRegistry.KeyPropertyNames);
    }

    public IMapper Build()
    {
        var type = _dynamicMethodBuilder.Build();
        var scalarTypeConverter = _mapperRegistry.MakeScalarTypeConverter();
        var listTypeConstructor = _mapperRegistry.MakeListTypeConstructor();
        var mapper = _mapperRegistry.MakeMapperSetLookUp(type);
        var proxy = _mapperRegistry.MakeEntityBaseProxy(type, scalarTypeConverter);
        var entityFactory = _mapperRegistry.MakeEntityFactory();

        // release some memory ahead
        _mapperRegistry.Clear();

        return new Mapper(scalarTypeConverter, listTypeConstructor, entityFactory, mapper, proxy);
    }

    public IMapperBuilder Register<TSource, TTarget>(ICustomPropertyMapper<TSource, TTarget>? customPropertyMapper = null)
        where TSource : class
        where TTarget : class
    {
        lock (_mapperRegistry)
        {
            var customPropertyMapperInternal = customPropertyMapper == null ? null : customPropertyMapper.ToInternal();
            _mapperRegistry.Register(typeof(TSource), typeof(TTarget), _dynamicMethodBuilder, customPropertyMapperInternal);
        }

        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>(
        ICustomPropertyMapper<TSource, TTarget>? customPropertyMapperSourceToTarget = null,
        ICustomPropertyMapper<TSource, TTarget>? customPropertyMapperTargetToSource = null)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (sourceType == targetType)
        {
            throw new SameTypeException(sourceType);
        }

        lock (_mapperRegistry)
        {
            var customPropertyMapperSourceToTargetInternal = customPropertyMapperSourceToTarget == null ? null : customPropertyMapperSourceToTarget.ToInternal();
            var customPropertyMapperTargetToSourceInternal = customPropertyMapperTargetToSource == null ? null : customPropertyMapperTargetToSource.ToInternal();
            _mapperRegistry.RegisterTwoWay(sourceType, targetType, _dynamicMethodBuilder, customPropertyMapperSourceToTargetInternal, customPropertyMapperTargetToSourceInternal);
        }

        return this;
    }

    IMapperBuilder IMapperBuilder.WithFactoryMethod<TList, TItem>(Expression<Func<TList>> factoryMethod, bool throwIfRedundant)
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithFactoryMethod(typeof(TList), typeof(TItem), factoryMethod.Compile(), throwIfRedundant);
        }

        return this;
    }

    public IMapperBuilder WithFactoryMethod<TEntity>(Expression<Func<TEntity>> factoryMethod, bool throwIfRedundant = false)
        where TEntity : class
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithFactoryMethod(typeof(TEntity), factoryMethod.Compile(), throwIfRedundant);
        }

        return this;
    }

    public IMapperBuilder WithConfiguration<TEntity>(TypeConfiguration configuration, bool throwIfRedundant = false)
        where TEntity : class
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithConfiguration(typeof(TEntity), configuration, _dynamicMethodBuilder, throwIfRedundant);
        }

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

        lock (_mapperRegistry)
        {
            _mapperRegistry.WithScalarConverter(typeof(TSource), typeof(TTarget), expression.Compile(), throwIfRedundant);
        }

        return this;
    }
}

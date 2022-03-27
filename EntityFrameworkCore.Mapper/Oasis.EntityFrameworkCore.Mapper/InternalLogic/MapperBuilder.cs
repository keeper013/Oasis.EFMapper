namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
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
        var mapper = _mapperRegistry.MakeMapperSetLookUp(type);
        var proxy = _mapperRegistry.MakeEntityBaseProxy(type, scalarTypeConverter);
        var entityFactory = _mapperRegistry.MakeEntityFactory();
        return new Mapper(scalarTypeConverter, entityFactory, mapper, proxy);
    }

    public IMapperBuilder Register<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.Register(typeof(TSource), typeof(TTarget), _dynamicMethodBuilder);
        }

        return this;
    }

    public IMapperBuilder RegisterTwoWay<TSource, TTarget>()
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
            _mapperRegistry.Register(sourceType, targetType, _dynamicMethodBuilder);
            _mapperRegistry.Register(targetType, sourceType, _dynamicMethodBuilder);
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

    public IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression, bool throwIfRedundant = false)
        where TSource : notnull
        where TTarget : notnull
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithScalarConverter(typeof(TSource), typeof(TTarget), expression.Compile(), throwIfRedundant);
        }

        return this;
    }
}

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly MapperRegistry _mapperRegistry;

    // TODO: add default configuration support
    public MapperBuilder(string assemblyName)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _mapperRegistry = new ();
        _dynamicMethodBuilder = new DynamicMethodBuilder(
            module.DefineType("Mapper", TypeAttributes.Public),
            _mapperRegistry.ScalarMapperTypeValidator,
            _mapperRegistry.EntityMapperTypeValidator,
            _mapperRegistry.EntityListMapperTypeValidator);
    }

    public IMapper Build(string? defaultIdPropertyName, string? defaultTimeStampPropertyName)
    {
        var type = _dynamicMethodBuilder.Build();
        var scalarTypeConverter = _mapperRegistry.MakeScalarTypeConverter();
        var mapper = _mapperRegistry.MakeMapperSetLookUp(type);
        var proxy = _mapperRegistry.MakeEntityBaseProxy(type, scalarTypeConverter);
        return new Mapper(scalarTypeConverter, mapper, proxy);
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

    public IMapperBuilder WithConfiguration<TEntity>(TypeConfiguration configuration)
        where TEntity : class
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithConfiguration(typeof(TEntity), configuration, _dynamicMethodBuilder);
        }

        return this;
    }

    public IMapperBuilder WithScalarConverter<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression)
        where TSource : notnull
        where TTarget : notnull
    {
        lock (_mapperRegistry)
        {
            _mapperRegistry.WithScalarConverter(typeof(TSource), typeof(TTarget), expression.Compile());
        }

        return this;
    }
}

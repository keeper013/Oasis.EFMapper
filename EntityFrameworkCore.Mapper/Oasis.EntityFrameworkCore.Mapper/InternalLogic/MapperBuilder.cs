﻿namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapEntityPropertiesMethod = 'e';
    private const char MapListPropertiesMethod = 'l';

    private readonly TypeBuilder _typeBuilder;
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapper;
    private readonly TypeConfigurationCache _typeConfigurationCache;
    private readonly ScalarConverterCache _scalarConverterCache;
    private readonly GenericMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethod("Convert", Utilities.PublicInstance)!);
    private readonly GenericMethodCache _entityPropertyMapperCache = new (typeof(IEntityPropertyMapper).GetMethod("MapEntityProperty", Utilities.PublicInstance)!);
    private readonly GenericMethodCache _listPropertyMapperCache = new (typeof(IListPropertyMapper).GetMethod("MapListProperty", Utilities.PublicInstance)!);

    public MapperBuilder(string assemblyName)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _typeBuilder = module.DefineType("Mapper", TypeAttributes.Public);
        _mapper = new ();
        _scalarConverterCache = new ();
        _typeConfigurationCache = new (_scalarConverterCache);
    }

    public IMapper Build(string? defaultIdPropertyName, string? defaultTimeStampPropertyName)
    {
        var type = _typeBuilder.CreateType();
        var mapper = new Dictionary<Type, IReadOnlyDictionary<Type, MapperSet>>();
        foreach (var pair in _mapper)
        {
            var innerMapper = new Dictionary<Type, MapperSet>();
            foreach (var innerPair in pair.Value)
            {
                var mapperMetaDataSet = innerPair.Value;
                var mapperSet = new MapperSet(
                    Delegate.CreateDelegate(mapperMetaDataSet.scalarPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.scalarPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.entityPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.entityPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.listPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.listPropertiesMapper.name)!));
                innerMapper.Add(innerPair.Key, mapperSet);
            }

            mapper.Add(pair.Key, innerMapper);
        }

        return new Mapper(_scalarConverterCache.Export(), mapper, new EntityBaseProxy(_typeConfigurationCache.Export()));
    }

    IMapperBuilder IMapperBuilder.Register<TSource, TTarget>()
    {
        // TypeConfigurationCache relies on ScalarConverterCache, so lock ScalarConverterCache instead
        lock (_scalarConverterCache)
        {
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TSource));
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TTarget));
        }

        var pathTracker = new RecursiveRegisterPathTracker(this);
        return RecursivelyRegister<TSource, TTarget>(pathTracker);
    }

    IMapperBuilder IMapperBuilder.RegisterTwoWay<TSource, TTarget>()
    {
        if (typeof(TSource) == typeof(TTarget))
        {
            throw new SameTypeException(typeof(TSource));
        }

        // TypeConfigurationCache relies on ScalarConverterCache, so lock ScalarConverterCache instead
        lock (_scalarConverterCache)
        {
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TSource));
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TTarget));
        }

        var pathTracker = new RecursiveRegisterPathTracker(this);
        RecursivelyRegister<TSource, TTarget>(pathTracker);
        return RecursivelyRegister<TTarget, TSource>(pathTracker);
    }

    public IMapperBuilder WithScalarMapper<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression)
    {
        // TypeConfigurationCache relies on ScalarConverterCache, so lock ScalarConverterCache instead
        lock (_scalarConverterCache)
        {
            _scalarConverterCache.Register(expression);
        }

        return this;
    }

    IMapperBuilder IMapperBuilder.WithConfiguration<T>(TypeConfiguration configuration)
    {
        lock (_scalarConverterCache)
        {
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(T));
        }

        return this;
    }

    private IMapperBuilder RecursivelyRegister<TSource, TTarget>(RecursiveRegisterPathTracker pathTracker)
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (!pathTracker.Contains(sourceType, targetType))
        {
            lock (_mapper)
            {
                pathTracker.Push(sourceType, targetType);
                if (!_mapper.TryGetValue(sourceType, out var innerDictionary))
                {
                    innerDictionary = new Dictionary<Type, MapperMetaDataSet>();
                    _mapper[sourceType] = innerDictionary;
                }

                if (!innerDictionary.ContainsKey(targetType))
                {
                    var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
                    var targetProperties = targetType.GetProperties(Utilities.PublicInstance);
                    innerDictionary[targetType] = new MapperMetaDataSet(
                        BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties),
                        BuildUpEntityPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties, pathTracker),
                        BuildUpListPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties, pathTracker));
                }

                pathTracker.Pop();
            }
        }

        return this;
    }

    private MapperMetaData BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] sourceProperties,
        PropertyInfo[] targetProperties)
        where TSource : class
        where TTarget : class
    {
        var method = InitializeDynamicMethod(sourceType, targetType, MapScalarPropertiesMethod, new[] { sourceType, targetType, typeof(IScalarTypeConverter) });
        FillScalarPropertiesMapper(method.GetILGenerator(), sourceProperties, targetProperties);

        return new MapperMetaData(typeof(Utilities.MapScalarProperties<TSource, TTarget>), method.Name);
    }

    private MapperMetaData BuildUpEntityPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] sourceProperties,
        PropertyInfo[] targetProperties,
        RecursiveRegisterPathTracker pathTracker)
        where TSource : class
        where TTarget : class
    {
        var method = InitializeDynamicMethod(sourceType, targetType, MapEntityPropertiesMethod, new[] { sourceType, targetType, typeof(IEntityPropertyMapper) });
        FillEntityPropertiesMapper(method.GetILGenerator(), sourceProperties, targetProperties, pathTracker);
        return new MapperMetaData(typeof(Utilities.MapEntityProperties<TSource, TTarget>), method.Name);
    }

    private MapperMetaData BuildUpListPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] sourceProperties,
        PropertyInfo[] targetProperties,
        RecursiveRegisterPathTracker pathTracker)
        where TSource : class
        where TTarget : class
    {
        var method = InitializeDynamicMethod(sourceType, targetType, MapListPropertiesMethod, new[] { sourceType, targetType, typeof(IListPropertyMapper) });
        FillListPropertiesMapper(method.GetILGenerator(), sourceProperties, targetProperties, pathTracker);

        return new MapperMetaData(typeof(Utilities.MapListProperties<TSource, TTarget>), method.Name);
    }

    private MethodBuilder InitializeDynamicMethod(Type sourceType, Type targetType, char type, Type[] parameterTypes)
    {
        var methodName = Utilities.BuildMethodName(type, sourceType, targetType);
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(typeof(void));

        return methodBuilder;
    }

    private void FillScalarPropertiesMapper(ILGenerator generator, PropertyInfo[] allSourceProperties, PropertyInfo[] allTargetProperties)
    {
        var sourceProperties = allSourceProperties.Where(p => p.IsScalarProperty(_scalarConverterCache.SourceTypes, true, false));
        var targetProperties = allTargetProperties.Where(p => p.IsScalarProperty(_scalarConverterCache.TargetTypes, true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            var sourcePropertyType = sourceProperty.PropertyType;
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty)
                && (sourcePropertyType == targetProperty.PropertyType || _scalarConverterCache.Contains(sourcePropertyType, targetProperty.PropertyType)))
            {
                var targetPropertyType = targetProperty.PropertyType;
                var needToConvert = sourcePropertyType != targetPropertyType;
                generator.Emit(OpCodes.Ldarg_1);
                if (needToConvert)
                {
                    generator.Emit(OpCodes.Ldarg_2);
                }

                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                if (needToConvert)
                {
                    generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourcePropertyType, targetPropertyType));
                }

                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void FillEntityPropertiesMapper(
        ILGenerator generator,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        RecursiveRegisterPathTracker pathTracker)
    {
        var sourceProperties = allSourceProperties.Where(p => p.IsEntityProperty(true, false));
        var targetProperties = allTargetProperties.Where(p => p.IsEntityProperty(true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourcePropertyType = sourceProperty.PropertyType;
                var targetPropertyType = targetProperty.PropertyType;

                // cascading mapper creation: if entity mapper doesn't exist, create it
                pathTracker.RegisterIf(sourcePropertyType, targetPropertyType, !_mapper.ItemExists(sourcePropertyType, targetPropertyType));

                // now it's made sure that mapper between entities exists, emit the entity property mapping code
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                generator.Emit(OpCodes.Callvirt, _entityPropertyMapperCache.CreateIfNotExist(sourcePropertyType, targetPropertyType));
                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void FillListPropertiesMapper(
        ILGenerator generator,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        RecursiveRegisterPathTracker pathTracker)
    {
        var sourceProperties = allSourceProperties.Where(p => p.IsListOfEntityProperty(true, false));
        var targetProperties = allTargetProperties.Where(p => p.IsListOfEntityProperty(true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourceItemType = sourceProperty.PropertyType.GenericTypeArguments[0];
                var targetItemType = targetProperty.PropertyType.GenericTypeArguments[0];

                // cascading mapper creation: if list item mapper doesn't exist, create it
                pathTracker.RegisterIf(sourceItemType, targetItemType, !_mapper.ItemExists(sourceItemType, targetItemType));

                // now it's made sure that mapper between list items exists, emit the list property mapping code
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                var jumpLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Brtrue_S, jumpLabel);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Newobj, typeof(List<>).MakeGenericType(targetProperty.PropertyType.GetGenericArguments()).GetConstructor(Array.Empty<Type>())!);
                generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
                generator.MarkLabel(jumpLabel);
                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                generator.Emit(OpCodes.Callvirt, _listPropertyMapperCache.CreateIfNotExist(sourceItemType, targetItemType));
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private record struct MapperMetaData(Type type, string name);

    private record struct MapperMetaDataSet(MapperMetaData scalarPropertiesMapper, MapperMetaData entityPropertiesMapper, MapperMetaData listPropertiesMapper);

    internal record struct EntityPropertyNameSet(string id, string timestamp);
}

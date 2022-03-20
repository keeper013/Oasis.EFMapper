namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class MapperBuilder : IMapperBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapEntityPropertiesMethod = 'e';
    private const char MapListPropertiesMethod = 'l';
    private const char CompareIdMethod = 'i';
    private const char CompareTimeStampMethod = 't';

    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, ComparerMetaDataSet>> _comparer = new ();
    private readonly TypeConfigurationCache _typeConfigurationCache;
    private readonly ScalarConverterCache _scalarConverterCache = new ();
    private readonly NullableTypeMethodCache _nullableTypeMethodCache = new ();
    private readonly GenericMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethod("Convert", Utilities.PublicInstance)!);
    private readonly GenericMethodCache _entityPropertyMapperCache = new (typeof(IEntityPropertyMapper).GetMethod("MapEntityProperty", Utilities.PublicInstance)!);
    private readonly GenericMethodCache _listPropertyMapperCache = new (typeof(IListPropertyMapper).GetMethod("MapListProperty", Utilities.PublicInstance)!);

    // TODO: add default configuration support
    public MapperBuilder(string assemblyName)
    {
        var name = new AssemblyName($"{assemblyName}.Oasis.EntityFrameworkCore.Mapper.Generated");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = assemblyBuilder.DefineDynamicModule($"{name.Name}.dll");
        _dynamicMethodBuilder = new DynamicMethodBuilder(module.DefineType("Mapper", TypeAttributes.Public));
        _typeConfigurationCache = new (_dynamicMethodBuilder, _scalarConverterCache);
    }

    public IMapper Build(string? defaultIdPropertyName, string? defaultTimeStampPropertyName)
    {
        var type = _dynamicMethodBuilder.Build();
        var mapper = new Dictionary<Type, IReadOnlyDictionary<Type, MapperSet>>();
        foreach (var pair in _mapper)
        {
            var innerDictionary = new Dictionary<Type, MapperSet>();
            foreach (var innerPair in pair.Value)
            {
                var mapperMetaDataSet = innerPair.Value;
                var mapperSet = new MapperSet(
                    Delegate.CreateDelegate(mapperMetaDataSet.scalarPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.scalarPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.entityPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.entityPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.listPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.listPropertiesMapper.name)!));
                innerDictionary.Add(innerPair.Key, mapperSet);
            }

            mapper.Add(pair.Key, innerDictionary);
        }

        var typeProxies = new Dictionary<Type, TypeProxy>();
        var proxies = _typeConfigurationCache.Export();
        foreach (var pair in proxies)
        {
            var typeMetaDataSet = pair.Value;
            var typeProxy = new TypeProxy(
                Delegate.CreateDelegate(typeMetaDataSet.getId.type, type!.GetMethod(typeMetaDataSet.getId.name)!),
                Delegate.CreateDelegate(typeMetaDataSet.identityIsEmpty.type, type!.GetMethod(typeMetaDataSet.identityIsEmpty.name)!),
                Delegate.CreateDelegate(typeMetaDataSet.timestampIsEmpty.type, type!.GetMethod(typeMetaDataSet.timestampIsEmpty.name)!),
                typeMetaDataSet.identityProperty,
                typeMetaDataSet.keepEntityOnMappingRemoved);
            typeProxies.Add(pair.Key, typeProxy);
        }

        var comparer = new Dictionary<Type, IReadOnlyDictionary<Type, EntityComparer>>();
        foreach (var pair in _comparer)
        {
            var innerDictionary = new Dictionary<Type, EntityComparer>();
            foreach (var innerPair in pair.Value)
            {
                var comparerMetaDataSet = innerPair.Value;
                var comparerSet = new EntityComparer(
                    Delegate.CreateDelegate(comparerMetaDataSet.identityComparer.type, type!.GetMethod(comparerMetaDataSet.identityComparer.name)!),
                    Delegate.CreateDelegate(comparerMetaDataSet.timeStampComparer.type, type!.GetMethod(comparerMetaDataSet.timeStampComparer.name)!));
                innerDictionary.Add(innerPair.Key, comparerSet);
            }

            comparer.Add(pair.Key, innerDictionary);
        }

        return new Mapper(_scalarConverterCache, mapper, new EntityBaseProxy(typeProxies, comparer, _scalarConverterCache));
    }

    IMapperBuilder IMapperBuilder.Register<TSource, TTarget>()
    {
        // TypeConfigurationCache relies on ScalarConverterCache, so lock ScalarConverterCache instead
        lock (_scalarConverterCache)
        {
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TSource), typeof(TTarget));
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
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TSource), typeof(TTarget));
            _typeConfigurationCache.ValidateEntityBaseProperties(typeof(TTarget), typeof(TSource));
        }

        var pathTracker = new RecursiveRegisterPathTracker(this);
        RecursivelyRegister<TSource, TTarget>(pathTracker);
        return RecursivelyRegister<TTarget, TSource>(pathTracker);
    }

    public IMapperBuilder WithScalarMapper<TSource, TTarget>(Expression<Func<TSource?, TTarget?>> expression)
        where TSource : notnull
        where TTarget : notnull
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
            _typeConfigurationCache.AddConfiguration(typeof(T), configuration);
        }

        return this;
    }

    private static string BuildMapperMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__MapTo__{targetType.FullName!.Replace(".", "_")}";
    }

    private static string BuildPropertyCompareMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__CompareTo__{targetType.FullName!.Replace(".", "_")}";
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
                Dictionary<Type, ComparerMetaDataSet>? innerDictionary2 = default;
                if (!_mapper.TryGetValue(sourceType, out var innerDictionary1))
                {
                    innerDictionary1 = new Dictionary<Type, MapperMetaDataSet>();
                    _mapper[sourceType] = innerDictionary1;
                    innerDictionary2 = new Dictionary<Type, ComparerMetaDataSet>();
                    _comparer[sourceType] = innerDictionary2;
                }

                if (!innerDictionary1.ContainsKey(targetType))
                {
                    var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
                    var targetProperties = targetType.GetProperties(Utilities.PublicInstance);
                    innerDictionary1[targetType] = new MapperMetaDataSet(
                        BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties),
                        BuildUpEntityPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties, pathTracker),
                        BuildUpListPropertiesMapperMethod<TSource, TTarget>(sourceType, targetType, sourceProperties, targetProperties, pathTracker));

                    var sourceIdProperty = sourceProperties.First(p => string.Equals(p.Name, _typeConfigurationCache.GetIdPropertyName<TSource>()));
                    var sourceTimeStampProperty = sourceProperties.First(p => string.Equals(p.Name, _typeConfigurationCache.GetTimeStampPropertyName<TSource>()));
                    var targetIdProperty = sourceProperties.First(p => string.Equals(p.Name, _typeConfigurationCache.GetIdPropertyName<TTarget>()));
                    var targetTimeStampProperty = sourceProperties.First(p => string.Equals(p.Name, _typeConfigurationCache.GetTimeStampPropertyName<TTarget>()));
                    innerDictionary2![targetType] = new ComparerMetaDataSet(
                        BuildUpIdEqualComparerMethod<TSource, TTarget>(sourceType, targetType, sourceIdProperty, targetIdProperty),
                        BuildUpTimeStampEqualComparerMethod<TSource, TTarget>(sourceType, targetType, sourceTimeStampProperty, targetTimeStampProperty));
                }

                pathTracker.Pop();
            }
        }

        return this;
    }

    private MethodMetaData BuildUpScalarPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties)
        where TSource : class
        where TTarget : class
    {
        var methodName = BuildMapperMethodName(MapScalarPropertiesMethod, sourceType, targetType);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
        var generator = method.GetILGenerator();
        var sourceProperties = allSourceProperties.Where(p => p.IsScalarProperty(_scalarConverterCache.SourceTypes, true, false));
        var targetProperties = allTargetProperties.Where(p => p.IsScalarProperty(_scalarConverterCache.TargetTypes, true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            var sourcePropertyType = sourceProperty.PropertyType;
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty)
                && (sourcePropertyType == targetProperty.PropertyType || _scalarConverterCache.CanConvert(sourcePropertyType, targetProperty.PropertyType)))
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

        return new MethodMetaData(typeof(Utilities.MapScalarProperties<TSource, TTarget>), method.Name);
    }

    private MethodMetaData BuildUpEntityPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        RecursiveRegisterPathTracker pathTracker)
        where TSource : class
        where TTarget : class
    {
        var methodName = BuildMapperMethodName(MapEntityPropertiesMethod, sourceType, targetType);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { sourceType, targetType, typeof(IEntityPropertyMapper) }, typeof(void));
        var generator = method.GetILGenerator();
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

        return new MethodMetaData(typeof(Utilities.MapEntityProperties<TSource, TTarget>), method.Name);
    }

    private MethodMetaData BuildUpListPropertiesMapperMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        RecursiveRegisterPathTracker pathTracker)
        where TSource : class
        where TTarget : class
    {
        var methodName = BuildMapperMethodName(MapListPropertiesMethod, sourceType, targetType);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { sourceType, targetType, typeof(IListPropertyMapper) }, typeof(void));
        var generator = method.GetILGenerator();
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

        return new MethodMetaData(typeof(Utilities.MapListProperties<TSource, TTarget>), method.Name);
    }

    private MethodMetaData BuildUpIdEqualComparerMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceIdProperty,
        PropertyInfo targetIdProperty)
        where TSource : class
        where TTarget : class
    {
        var methodName = BuildPropertyCompareMethodName(CompareIdMethod, sourceType, targetType);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        if (sourceIdProperty.PropertyType == targetIdProperty.PropertyType)
        {
            if (targetType.IsPrimitive)
            {
                GeneratePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetType.IsNullablePrimitive())
            {
                GenerateNullablePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetType == Utilities.StringType)
            {
                GenerateStringEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else
            {
                GenerateObjectEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
        }
        else
        {
            if (targetType.IsPrimitive)
            {
                GenerateConvertedPrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetType.IsNullablePrimitive())
            {
                GenerateConvertedNullablePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetType == Utilities.StringType)
            {
                GenerateConvertedStringEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else
            {
                GenerateConvertedObjectEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
        }

        return new MethodMetaData(typeof(Utilities.IdsAreEqual<TSource, TTarget>), method.Name);
    }

    private MethodMetaData BuildUpTimeStampEqualComparerMethod<TSource, TTarget>(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceTimeStampProperty,
        PropertyInfo targetTimeStampProperty)
        where TSource : class
        where TTarget : class
    {
        var methodName = BuildPropertyCompareMethodName(CompareTimeStampMethod, sourceType, targetType);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        if (sourceTimeStampProperty.PropertyType == targetTimeStampProperty.PropertyType)
        {
            if (targetType.IsPrimitive)
            {
                GeneratePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType.IsNullablePrimitive())
            {
                GenerateNullablePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType == Utilities.StringType)
            {
                GenerateStringEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType == Utilities.ByteArrayType)
            {
                GenerateByteArrayEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else
            {
                GenerateObjectEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
        }
        else
        {
            if (targetType.IsPrimitive)
            {
                GenerateConvertedPrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType.IsNullablePrimitive())
            {
                GenerateConvertedNullablePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType == Utilities.StringType)
            {
                GenerateConvertedStringEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetType == Utilities.ByteArrayType)
            {
                GenerateConvertedByteArrayEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else
            {
                GenerateConvertedObjectEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
        }

        return new MethodMetaData(typeof(Utilities.TimeStampsAreEqual<TSource, TTarget>), method.Name);
    }

    private void GeneratePrimitiveEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateConvertedPrimitiveEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var local0Type = typeof(Nullable<>).MakeGenericType(targetProperty.PropertyType);
        generator.DeclareLocal(local0Type);
        generator.DeclareLocal(targetProperty.PropertyType);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourceProperty.PropertyType, targetProperty.PropertyType));
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_1);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(local0Type, NullableTypeMethodCache.GetValueOrDefault));
        generator.Emit(OpCodes.Ldloc_1);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(local0Type, NullableTypeMethodCache.HasValue));
        generator.Emit(OpCodes.And);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateNullablePrimitiveEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var sourcePropertyType = sourceProperty.PropertyType;
        var targetPropertyType = targetProperty.PropertyType;
        generator.DeclareLocal(sourcePropertyType);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(sourcePropertyType, NullableTypeMethodCache.HasValue));
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.HasValue));
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(sourcePropertyType, NullableTypeMethodCache.Value));
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.Value));
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateConvertedNullablePrimitiveEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateStringEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateConvertedStringEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateByteArrayEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateConvertedByteArrayEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateObjectEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private void GenerateConvertedObjectEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
    }

    private record struct MapperMetaDataSet(MethodMetaData scalarPropertiesMapper, MethodMetaData entityPropertiesMapper, MethodMetaData listPropertiesMapper);

    // TODO: timestamp property may not exist
    private record struct ComparerMetaDataSet(MethodMetaData identityComparer, MethodMetaData timeStampComparer);
}

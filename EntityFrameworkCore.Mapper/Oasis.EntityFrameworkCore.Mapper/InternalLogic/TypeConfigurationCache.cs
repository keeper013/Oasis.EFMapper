namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class TypeConfigurationCache
{
    private const char GetId = 'g';
    private const char IdEmpty = 'i';
    private const char TimeStampEmpty = 't';
    private static readonly MethodInfo StringIsNullOrEmpty = typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!;
    private static readonly MethodInfo EnumerableAny = GetEnumerableAny();
    private static readonly Type LongType = typeof(long);
    private static readonly Type DoubleType = typeof(double);
    private static readonly Type UlongType = typeof(ulong);

    private readonly IDynamicMethodBuilder _dynamicMethodBuilder;
    private readonly ScalarConverterCache _scalarConverterCache;
    private readonly NullableTypeMethodCache _nullableTypeMethodCache;
    private readonly Dictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeProxyMetaDataSet> _typeProxies = new ();

    public TypeConfigurationCache(
        IDynamicMethodBuilder dynamicMethodBuilder,
        ScalarConverterCache scalarConverterCache,
        NullableTypeMethodCache nullableTypeMethodCache)
    {
        _dynamicMethodBuilder = dynamicMethodBuilder;
        _scalarConverterCache = scalarConverterCache;
        _nullableTypeMethodCache = nullableTypeMethodCache;
    }

    public IReadOnlyDictionary<Type, TypeProxyMetaDataSet> Export() => _typeProxies;

    public void AddConfiguration<TEntity>(TypeConfiguration configuration)
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_typesUsingCustomConfiguration.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        if (_typesUsingDefaultConfiguration.Contains(type))
        {
            throw new TypeAlreadyRegisteredException(type);
        }

        var identityPropertyName = configuration.GetIdPropertyName();
        var timeStampPropertyName = configuration.GetTimestampPropertyName();
        GetEntityBaseProperties(type, identityPropertyName, timeStampPropertyName, out var identityProperty, out var timeStampProperty);

        _typesUsingCustomConfiguration[type] = configuration;
        _typeProxies.Add(type, BuildTypeProxy<TEntity>(type, identityProperty, timeStampProperty, configuration.keepEntityOnMappingRemoved));
    }

    public void ValidateEntityBaseProperties<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        var sourceTypeValidated = _typesUsingCustomConfiguration.ContainsKey(sourceType) || _typesUsingDefaultConfiguration.Contains(sourceType);
        var targetTypeValidated = _typesUsingCustomConfiguration.ContainsKey(targetType) || _typesUsingDefaultConfiguration.Contains(targetType);
        if (!sourceTypeValidated || !targetTypeValidated)
        {
            GetEntityBaseProperties(sourceType, GetIdPropertyName(sourceType), GetTimeStampPropertyName(sourceType), out var sourceIdProperty, out var sourceTimeStampProperty);
            GetEntityBaseProperties(targetType, GetIdPropertyName(targetType), GetTimeStampPropertyName(targetType), out var targetIdProperty, out var targetTimeStampProperty);

            var sourceIdType = sourceIdProperty.PropertyType;
            var targetIdType = targetIdProperty.PropertyType;
            if (sourceIdType != targetIdType && !_scalarConverterCache.CanConvert(sourceIdType, targetIdType))
            {
                throw new ScalarConverterMissingException(sourceIdType, targetIdType);
            }

            var sourceTimeStampType = sourceTimeStampProperty.PropertyType;
            var targetTimeStampType = targetTimeStampProperty.PropertyType;
            if (sourceTimeStampType != targetTimeStampType && !_scalarConverterCache.CanConvert(sourceTimeStampType, targetTimeStampType))
            {
                throw new ScalarConverterMissingException(sourceTimeStampType, targetTimeStampType);
            }

            if (!sourceTypeValidated)
            {
                _typesUsingDefaultConfiguration.Add(sourceType);
                _typeProxies.Add(sourceType, BuildTypeProxy<TSource>(sourceType, sourceIdProperty, sourceTimeStampProperty));
            }

            if (!targetTypeValidated)
            {
                _typesUsingDefaultConfiguration.Add(targetType);
                _typeProxies.Add(targetType, BuildTypeProxy<TTarget>(targetType, targetIdProperty, targetTimeStampProperty));
            }
        }
    }

    public string GetIdPropertyName<TEntity>()
    {
        return GetIdPropertyName(typeof(TEntity));
    }

    public string GetTimeStampPropertyName<TEntity>()
    {
        return GetTimeStampPropertyName(typeof(TEntity));
    }

    private static MethodInfo GetEnumerableAny()
    {
        var enumerableSequenceEqual = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.Name.Contains(nameof(Enumerable.Any)))
            .Single(x => x.GetParameters().Length == 1);
        return enumerableSequenceEqual.MakeGenericMethod(typeof(byte));
    }

    private static string BuildMethodName(char prefix, Type entityType)
    {
        return $"_{prefix}__{entityType.FullName!.Replace(".", "_")}";
    }

    private string GetIdPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetIdPropertyName() : Utilities.DefaultIdPropertyName;
    }

    private string GetTimeStampPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetTimestampPropertyName() : Utilities.DefaultTimestampPropertyName;
    }

    private TypeProxyMetaDataSet BuildTypeProxy<TEntity>(
        Type type,
        PropertyInfo identityProperty,
        PropertyInfo timeStampProperty,
        bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved)
        where TEntity : class
    {
        return new TypeProxyMetaDataSet(
            BuildUpGetIdMethod<TEntity>(type, identityProperty),
            BuildUpIdIsEmptyMethod<TEntity>(type, identityProperty),
            BuildUpTimeStampIsEmptyMethod<TEntity>(type, timeStampProperty),
            identityProperty,
            keepEntityOnMappingRemoved);
    }

    private MethodMetaData BuildUpGetIdMethod<TEntity>(Type type, PropertyInfo identityProperty)
        where TEntity : class
    {
        var methodName = BuildMethodName(GetId, type);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { type }, typeof(object));
        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
        var identityPropertyType = identityProperty.PropertyType;
        if (identityPropertyType.IsPrimitive || identityPropertyType.IsNullablePrimitive())
        {
            generator.Emit(OpCodes.Box, identityPropertyType);
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetId<TEntity>), method.Name);
    }

    private MethodMetaData BuildUpIdIsEmptyMethod<TEntity>(Type type, PropertyInfo identityProperty)
        where TEntity : class
    {
        var methodName = BuildMethodName(IdEmpty, type);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { type }, typeof(bool));
        var identityPropertyType = identityProperty.PropertyType;
        var generator = method.GetILGenerator();

        if (identityPropertyType.IsPrimitive)
        {
            GeneratePrimitiveIsEmptyIL(generator, identityProperty);
        }
        else if (identityPropertyType.IsNullablePrimitive())
        {
            GenerateNullablePrimitiveIsEmptyIL(generator, identityProperty);
        }
        else if (identityPropertyType == Utilities.StringType)
        {
            GenerateStringIsEmptyIL(generator, identityProperty);
        }
        else
        {
            GenerateObjectIsEmptyIL(generator, identityProperty);
        }

        return new MethodMetaData(typeof(Utilities.IdIsEmpty<TEntity>), method.Name);
    }

    private MethodMetaData BuildUpTimeStampIsEmptyMethod<TEntity>(Type type, PropertyInfo timeStampProperty)
        where TEntity : class
    {
        var methodName = BuildMethodName(TimeStampEmpty, type);
        var method = _dynamicMethodBuilder.Build(methodName, new[] { type }, typeof(bool));
        var timeStampPropertyType = timeStampProperty.PropertyType;
        var generator = method.GetILGenerator();

        if (timeStampPropertyType.IsPrimitive)
        {
            GeneratePrimitiveIsEmptyIL(generator, timeStampProperty);
        }
        else if (timeStampPropertyType.IsNullablePrimitive())
        {
            GenerateNullablePrimitiveIsEmptyIL(generator, timeStampProperty);
        }
        else if (timeStampPropertyType == Utilities.StringType)
        {
            GenerateStringIsEmptyIL(generator, timeStampProperty);
        }
        else if (timeStampPropertyType == Utilities.ByteArrayType)
        {
            GenerateByteArrayIsEmptyIL(generator, timeStampProperty);
        }
        else
        {
            GenerateObjectIsEmptyIL(generator, timeStampProperty);
        }

        return new MethodMetaData(typeof(Utilities.TimeStampIsEmpty<TEntity>), method.Name);
    }

    private void GeneratePrimitiveIsEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        generator.Emit(OpCodes.Ldc_I4_0);
        var propertyType = property.PropertyType;
        generator.Emit(OpCodes.Conv_I8);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateNullablePrimitiveIsEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        generator.DeclareLocal(property.PropertyType);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(property.PropertyType, NullableTypeMethodCache.HasValue));
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateObjectIsEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        generator.Emit(OpCodes.Ldnull);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateByteArrayIsEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        generator.Emit(OpCodes.Call, EnumerableAny);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_1);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateStringIsEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, property.GetMethod!);
        generator.Emit(OpCodes.Call, StringIsNullOrEmpty);
        generator.Emit(OpCodes.Ret);
    }

    private void GetEntityBaseProperties(
        Type type,
        string identityPropertyName,
        string timestampPropertyName,
        out PropertyInfo identityProperty,
        out PropertyInfo timestampProperty)
    {
        var properties = type.GetProperties(Utilities.PublicInstance).Where(p => p.GetMethod != null && p.SetMethod != null);
        var tempIdentityProperty = properties.SingleOrDefault(p => string.Equals(p.Name, identityPropertyName));
        var tempTimeStampProperty = properties.SingleOrDefault(p => string.Equals(p.Name, timestampPropertyName));
        if (tempIdentityProperty == null)
        {
            throw new InvalidEntityBasePropertyException(type, "id", identityPropertyName);
        }

        if (tempTimeStampProperty == null)
        {
            throw new InvalidEntityBasePropertyException(type, "timestamp", timestampPropertyName);
        }

        identityProperty = tempIdentityProperty;
        timestampProperty = tempTimeStampProperty;
    }
}

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;
using System.Reflection.Emit;

internal interface IDynamicMethodBuilder
{
    MethodMetaData BuildUpScalarPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties);

    MethodMetaData BuildUpEntityPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        IRecursiveRegisterTrigger trigger);

    public MethodMetaData BuildUpEntityListPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        IRecursiveRegisterTrigger trigger);

    MethodMetaData BuildUpIdEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceIdProperty,
        PropertyInfo targetIdProperty);

    MethodMetaData BuildUpTimeStampEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceTimeStampProperty,
        PropertyInfo targetTimeStampProperty);

    MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty);

    MethodMetaData BuildUpIdIsEmptyMethod(Type type, PropertyInfo identityProperty);

    MethodMetaData BuildUpTimeStampIsEmptyMethod(Type type, PropertyInfo timeStampProperty);
}

internal sealed class DynamicMethodBuilder : IDynamicMethodBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapEntityPropertiesMethod = 'e';
    private const char MapListPropertiesMethod = 'l';
    private const char CompareIdMethod = 'i';
    private const char CompareTimeStampMethod = 't';
    private const char GetId = 'g';
    private const char IdEmpty = 'i';
    private const char TimeStampEmpty = 't';

    private static readonly Type StringType = typeof(string);
    private static readonly Type ByteArrayType = typeof(byte[]);

    private static readonly MethodInfo StringEqual = typeof(string).GetMethod(nameof(string.Equals), new[] { typeof(string), typeof(string) })!;
    private static readonly MethodInfo ByteArraySequenceEqual = GetByteArraySequenceEqual();
    private static readonly MethodInfo ObjectEqual = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object), typeof(object) })!;
    private static readonly MethodInfo StringIsNullOrEmpty = typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!;
    private static readonly MethodInfo EnumerableAny = GetEnumerableAny();

    private readonly GenericMapperMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethod(nameof(IScalarTypeConverter.Convert), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _entityPropertyMapperCache = new (typeof(IEntityPropertyMapper).GetMethod(nameof(IEntityPropertyMapper.MapEntityProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _listPropertyMapperCache = new (typeof(IListPropertyMapper).GetMethod(nameof(IListPropertyMapper.MapListProperty), Utilities.PublicInstance)!);
    private readonly NullableTypeMethodCache _nullableTypeMethodCache = new ();
    private readonly TypeBuilder _typeBuilder;
    private readonly IMapperTypeValidator _scalarTypeValidator;
    private readonly IMapperTypeValidator _entityTypeValidator;
    private readonly IMapperTypeValidator _entityListTypeValidator;

    public DynamicMethodBuilder(
        TypeBuilder typeBuilder,
        IMapperTypeValidator scalarTypeValidator,
        IMapperTypeValidator entityTypeValidator,
        IMapperTypeValidator entityListTypeValidator)
    {
        _typeBuilder = typeBuilder;
        _scalarTypeValidator = scalarTypeValidator;
        _entityTypeValidator = entityTypeValidator;
        _entityListTypeValidator = entityListTypeValidator;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData BuildUpScalarPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties)
    {
        var methodName = BuildMapperMethodName(MapScalarPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
        var generator = method.GetILGenerator();
        var sourceProperties = allSourceProperties.Where(p => _scalarTypeValidator.IsSourceType(p.PropertyType) && VerifyProperty(p, true, false));
        var targetProperties = allTargetProperties.Where(p => _scalarTypeValidator.IsSourceType(p.PropertyType) && VerifyProperty(p, true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            var sourcePropertyType = sourceProperty.PropertyType;
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty)
                && (sourcePropertyType == targetProperty.PropertyType || _scalarTypeValidator.CanConvert(sourcePropertyType, targetProperty.PropertyType)))
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

        return new MethodMetaData(typeof(Utilities.MapScalarProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpEntityPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        IRecursiveRegisterTrigger trigger)
    {
        var methodName = BuildMapperMethodName(MapEntityPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IEntityPropertyMapper) }, typeof(void));
        var generator = method.GetILGenerator();
        var sourceProperties = allSourceProperties.Where(p => _entityTypeValidator.IsSourceType(p.PropertyType) && VerifyProperty(p, true, false));
        var targetProperties = allTargetProperties.Where(p => _entityTypeValidator.IsTargetType(p.PropertyType) && VerifyProperty(p, true, true)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourcePropertyType = sourceProperty.PropertyType;
                var targetPropertyType = targetProperty.PropertyType;

                // cascading mapper creation: if entity mapper doesn't exist, create it
                trigger.RegisterIf(sourcePropertyType, targetPropertyType, !_entityTypeValidator.CanConvert(sourcePropertyType, targetPropertyType));

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
        return new MethodMetaData(typeof(Utilities.MapEntityProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpEntityListPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties,
        IRecursiveRegisterTrigger trigger)
    {
        var methodName = BuildMapperMethodName(MapListPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IListPropertyMapper) }, typeof(void));
        var generator = method.GetILGenerator();
        var sourceProperties = allSourceProperties.Where(p => _entityListTypeValidator.IsSourceType(p.PropertyType) && VerifyProperty(p, true, false));
        var targetProperties = allTargetProperties.Where(p => _entityListTypeValidator.IsTargetType(p.PropertyType) && VerifyProperty(p, true, false)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourceItemType = sourceProperty.PropertyType.GenericTypeArguments[0];
                var targetItemType = targetProperty.PropertyType.GenericTypeArguments[0];

                // cascading mapper creation: if list item mapper doesn't exist, create it
                trigger.RegisterIf(sourceItemType, targetItemType, !_entityListTypeValidator.CanConvert(sourceItemType, targetItemType));

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

        return new MethodMetaData(typeof(Utilities.MapListProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpIdEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceIdProperty,
        PropertyInfo targetIdProperty)
    {
        var methodName = BuildPropertyCompareMethodName(CompareIdMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        var sourcePropertyType = sourceIdProperty.PropertyType;
        var targetPropertyType = targetIdProperty.PropertyType;
        if (sourcePropertyType == targetPropertyType)
        {
            if (targetPropertyType.IsPrimitive)
            {
                GeneratePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetPropertyType.IsNullablePrimitive())
            {
                GenerateNullablePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetPropertyType == StringType)
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
            if (targetPropertyType.IsPrimitive)
            {
                GenerateConvertedPrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetPropertyType.IsNullablePrimitive())
            {
                GenerateConvertedNullablePrimitiveEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else if (targetPropertyType == StringType)
            {
                GenerateConvertedStringEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
            else
            {
                GenerateConvertedObjectEqualIL(generator, sourceIdProperty, targetIdProperty);
            }
        }

        return new MethodMetaData(typeof(Utilities.IdsAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpTimeStampEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceTimeStampProperty,
        PropertyInfo targetTimeStampProperty)
    {
        var methodName = BuildPropertyCompareMethodName(CompareTimeStampMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        var sourcePropertyType = sourceTimeStampProperty.PropertyType;
        var targetPropertyType = targetTimeStampProperty.PropertyType;
        if (sourcePropertyType == targetPropertyType)
        {
            if (targetPropertyType.IsPrimitive)
            {
                GeneratePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType.IsNullablePrimitive())
            {
                GenerateNullablePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType == StringType)
            {
                GenerateStringEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType == ByteArrayType)
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
            if (targetPropertyType.IsPrimitive)
            {
                GenerateConvertedPrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType.IsNullablePrimitive())
            {
                GenerateConvertedNullablePrimitiveEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType == StringType)
            {
                GenerateConvertedStringEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else if (targetPropertyType == ByteArrayType)
            {
                GenerateConvertedByteArrayEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
            else
            {
                GenerateConvertedObjectEqualIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
            }
        }

        return new MethodMetaData(typeof(Utilities.TimeStampsAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(GetId, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(object));
        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
        var identityPropertyType = identityProperty.PropertyType;
        if (identityPropertyType.IsPrimitive || identityPropertyType.IsNullablePrimitive())
        {
            generator.Emit(OpCodes.Box, identityPropertyType);
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetId<>).MakeGenericType(type), method.Name);
    }

    public MethodMetaData BuildUpIdIsEmptyMethod(Type type, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(IdEmpty, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(bool));
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
        else if (identityPropertyType == StringType)
        {
            GenerateStringIsEmptyIL(generator, identityProperty);
        }
        else
        {
            GenerateObjectIsEmptyIL(generator, identityProperty);
        }

        return new MethodMetaData(typeof(Utilities.IdIsEmpty<>).MakeGenericType(type), method.Name);
    }

    public MethodMetaData BuildUpTimeStampIsEmptyMethod(Type type, PropertyInfo timeStampProperty)
    {
        var methodName = BuildMethodName(TimeStampEmpty, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(bool));
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
        else if (timeStampPropertyType == StringType)
        {
            GenerateStringIsEmptyIL(generator, timeStampProperty);
        }
        else if (timeStampPropertyType == ByteArrayType)
        {
            GenerateByteArrayIsEmptyIL(generator, timeStampProperty);
        }
        else
        {
            GenerateObjectIsEmptyIL(generator, timeStampProperty);
        }

        return new MethodMetaData(typeof(Utilities.TimeStampIsEmpty<>).MakeGenericType(type), method.Name);
    }

    private static string BuildMethodName(char prefix, Type entityType)
    {
        return $"_{prefix}__{entityType.FullName!.Replace(".", "_")}";
    }

    private static string BuildMapperMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__MapTo__{targetType.FullName!.Replace(".", "_")}";
    }

    private static string BuildPropertyCompareMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{sourceType.FullName!.Replace(".", "_")}__CompareTo__{targetType.FullName!.Replace(".", "_")}";
    }

    private static MethodInfo GetEnumerableAny()
    {
        var enumerableSequenceEqual = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.Name.Contains(nameof(Enumerable.Any)))
            .Single(x => x.GetParameters().Length == 1);
        return enumerableSequenceEqual.MakeGenericMethod(typeof(byte));
    }

    private static MethodInfo GetByteArraySequenceEqual()
    {
        var enumerableSequenceEqual = typeof(Enumerable)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(x => x.Name.Contains(nameof(Enumerable.SequenceEqual)))
            .Single(x => x.GetParameters().Length == 2);
        return enumerableSequenceEqual.MakeGenericMethod(typeof(byte));
    }

    private static bool VerifyProperty(PropertyInfo prop, bool mustHaveGetter, bool mustHaveSetter)
    {
        return (!mustHaveGetter || prop.GetMethod != default) && (!mustHaveSetter || prop.SetMethod != default);
    }

    private MethodBuilder BuildMethod(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
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
        var targetPropertyType = targetProperty.PropertyType;
        generator.DeclareLocal(targetPropertyType);
        generator.DeclareLocal(targetPropertyType);

        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourceProperty.PropertyType, targetProperty.PropertyType));
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.HasValue));
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_1);
        generator.Emit(OpCodes.Ldloca_S, 1);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.HasValue));
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldloca_S, 0);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.Value));
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Stloc_1);
        generator.Emit(OpCodes.Ldloca_S, 1);
        generator.Emit(OpCodes.Call, _nullableTypeMethodCache.CreateIfNotExist(targetPropertyType, NullableTypeMethodCache.Value));
        generator.Emit(OpCodes.Ceq);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateStringEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, StringEqual);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateConvertedStringEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
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
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, StringEqual);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateByteArrayEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, ByteArraySequenceEqual);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateConvertedByteArrayEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.DeclareLocal(targetProperty.PropertyType);

        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourceProperty.PropertyType, targetProperty.PropertyType));
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloc_0);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldloc_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, ByteArraySequenceEqual);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateObjectEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, ObjectEqual);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
    }

    private void GenerateConvertedObjectEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        generator.DeclareLocal(targetProperty.PropertyType);

        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
        generator.Emit(OpCodes.Call, _scalarPropertyConverterCache.CreateIfNotExist(sourceProperty.PropertyType, targetProperty.PropertyType));
        generator.Emit(OpCodes.Stloc_0);
        generator.Emit(OpCodes.Ldloc_0);
        var jumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        generator.Emit(OpCodes.Ldloc_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        generator.Emit(OpCodes.Call, ObjectEqual);
        generator.Emit(OpCodes.Ret);
        generator.MarkLabel(jumpLabel);
        generator.Emit(OpCodes.Ldc_I4_0);
        generator.Emit(OpCodes.Ret);
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
}

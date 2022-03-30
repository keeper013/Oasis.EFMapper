namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;
using System.Reflection.Emit;

internal interface IDynamicMethodBuilder
{
    MethodMetaData BuildUpKeyPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties);

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

    MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty);

    MethodMetaData BuildUpIdIsEmptyMethod(Type type, PropertyInfo identityProperty);
}

internal sealed class DynamicMethodBuilder : IDynamicMethodBuilder
{
    private const char MapScalarPropertiesMethod = 's';
    private const char MapKeyPropertiesMethod = 'k';
    private const char MapEntityPropertiesMethod = 'e';
    private const char MapListPropertiesMethod = 'l';
    private const char CompareIdMethod = 'c';
    private const char GetId = 'g';
    private const char IdEmpty = 'i';

    private static readonly MethodInfo ObjectEqual = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) })!;
    private static readonly ConstructorInfo MissingSetting = typeof(SetterMissingException).GetConstructor(Utilities.PublicInstance, new[] { typeof(string) })!;

    private readonly GenericMapperMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethods().Single(m => string.Equals(m.Name, nameof(IScalarTypeConverter.Convert)) && m.IsGenericMethod));
    private readonly GenericMapperMethodCache _entityPropertyMapperCache = new (typeof(IEntityPropertyMapper).GetMethod(nameof(IEntityPropertyMapper.MapEntityProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _entityListPropertyMapperCache = new (typeof(IListPropertyMapper).GetMethod(nameof(IListPropertyMapper.MapListProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _listTypeConstructorCache = new (typeof(IListPropertyMapper).GetMethod(nameof(IListPropertyMapper.ConstructListType), Utilities.PublicInstance)!);
    private readonly NullableTypeMethodCache _nullableTypeMethodCache = new ();
    private readonly IScalarTypeMethodCache _isDefaultValueCache = new ScalarTypeMethodCache(typeof(ScalarTypeIsDefaultValueMethods), nameof(ScalarTypeIsDefaultValueMethods.IsDefaultValue), new[] { typeof(object) });
    private readonly IScalarTypeMethodCache _areEqualCache = new ScalarTypeMethodCache(typeof(ScalarTypeEqualMethods), nameof(ScalarTypeEqualMethods.AreEqual), new[] { typeof(object), typeof(object) });
    private readonly TypeBuilder _typeBuilder;
    private readonly IMapperTypeValidator _scalarTypeValidator;
    private readonly IMapperTypeValidator _entityTypeValidator;
    private readonly IMapperTypeValidator _entityListTypeValidator;
    private readonly IKeyPropertyNameManager _keyPropertyNameManager;

    public DynamicMethodBuilder(
        TypeBuilder typeBuilder,
        IMapperTypeValidator scalarTypeValidator,
        IMapperTypeValidator entityTypeValidator,
        IMapperTypeValidator entityListTypeValidator,
        IKeyPropertyNameManager keyPropertyNameManager)
    {
        _typeBuilder = typeBuilder;
        _scalarTypeValidator = scalarTypeValidator;
        _entityTypeValidator = entityTypeValidator;
        _entityListTypeValidator = entityListTypeValidator;
        _keyPropertyNameManager = keyPropertyNameManager;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData BuildUpKeyPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo[] allSourceProperties,
        PropertyInfo[] allTargetProperties)
    {
        var methodName = BuildMapperMethodName(MapKeyPropertiesMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
        var generator = method.GetILGenerator();
        var sourceIdentityProperty = allSourceProperties.GetProperty(_keyPropertyNameManager.GetIdentityPropertyName(sourceType));
        var targetIdentityProperty = allTargetProperties.GetProperty(_keyPropertyNameManager.GetIdentityPropertyName(targetType));

        if (sourceIdentityProperty != default && targetIdentityProperty != default)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, sourceIdentityProperty, targetIdentityProperty);
        }

        var sourceTimeStampProperty = allSourceProperties.GetProperty(_keyPropertyNameManager.GetTimeStampPropertyName(sourceType));
        var targetTimeStampProperty = allTargetProperties.GetProperty(_keyPropertyNameManager.GetTimeStampPropertyName(targetType));
        if (sourceTimeStampProperty != default && targetTimeStampProperty != default)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, sourceTimeStampProperty, targetTimeStampProperty);
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.MapScalarProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
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
        var sourceProperties = allSourceProperties
            .Where(p => _scalarTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true) && !_keyPropertyNameManager.IsKeyPropertyName(p.Name, sourceType));
        var targetProperties = allTargetProperties
            .Where(p => _scalarTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true) && !_keyPropertyNameManager.IsKeyPropertyName(p.Name, targetType))
            .ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty)
                && (sourceProperty.PropertyType == targetProperty.PropertyType || _scalarTypeValidator.CanConvert(sourceProperty.PropertyType, targetProperty.PropertyType)))
            {
                GenerateScalarPropertyValueAssignmentIL(generator, sourceProperty, targetProperty);
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
        var sourceProperties = allSourceProperties.Where(p => _entityTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true));
        var targetProperties = allTargetProperties.Where(p => _entityTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true)).ToDictionary(p => p.Name, p => p);

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
        var sourceProperties = allSourceProperties.Where(p => _entityListTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetProperties = allTargetProperties.Where(p => _entityListTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false)).ToDictionary(p => p.Name, p => p);

        foreach (var sourceProperty in sourceProperties)
        {
            if (targetProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourceItemType = sourceProperty.PropertyType.GetListItemType();
                var targetItemType = targetProperty.PropertyType.GetListItemType();

                // cascading mapper creation: if list item mapper doesn't exist, create it
                trigger.RegisterIf(sourceItemType!, targetItemType!, !_entityListTypeValidator.CanConvert(sourceItemType!, targetItemType!));

                // now it's made sure that mapper between list items exists, emit the list property mapping code
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                var jumpLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Brtrue_S, jumpLabel);
                if (targetProperty.SetMethod != default)
                {
                    generator.Emit(OpCodes.Ldarg_1);
                    generator.Emit(OpCodes.Ldarg_2);
                    generator.Emit(OpCodes.Callvirt, _listTypeConstructorCache.CreateIfNotExist(targetProperty.PropertyType, targetItemType!));
                    generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
                }
                else
                {
                    generator.Emit(OpCodes.Ldstr, $"Entity type: {targetType}, property name: {targetProperty.Name}, value is empty and the property doesn't have a setter.");
                    generator.Emit(OpCodes.Newobj, MissingSetting);
                    generator.Emit(OpCodes.Throw);
                }

                generator.MarkLabel(jumpLabel);

                generator.Emit(OpCodes.Ldarg_2);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
                generator.Emit(OpCodes.Callvirt, _entityListPropertyMapperCache.CreateIfNotExist(sourceItemType!, targetItemType!));
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
        GenerateScalarPropertyEqualIL(generator, sourceIdProperty, targetIdProperty);
        return new MethodMetaData(typeof(Utilities.IdsAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(GetId, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(object));
        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
        var identityPropertyType = identityProperty.PropertyType;
        if (identityPropertyType.IsValueType)
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
        var generator = method.GetILGenerator();
        GenerateScalarPropertyEmptyIL(generator, identityProperty);
        return new MethodMetaData(typeof(Utilities.IdIsEmpty<>).MakeGenericType(type), method.Name);
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

    private MethodBuilder BuildMethod(string methodName, Type[] parameterTypes, Type returnType)
    {
        var methodBuilder = _typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Static);
        methodBuilder.SetParameters(parameterTypes);
        methodBuilder.SetReturnType(returnType);

        return methodBuilder;
    }

    private void GenerateScalarPropertyValueAssignmentIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var sourcePropertyType = sourceProperty.PropertyType;
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

    private void GenerateScalarPropertyEmptyIL(ILGenerator generator, PropertyInfo property)
    {
        var propertyType = property.PropertyType;
        var methodInfo = _isDefaultValueCache.GetMethodFor(propertyType);
        if (methodInfo != default)
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, property.GetMethod!);
            generator.Emit(OpCodes.Call, methodInfo);
        }
        else
        {
            if (propertyType.IsValueType)
            {
                if (propertyType.IsPrimitive)
                {
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                    generator.Emit(OpCodes.Ldc_I4_0);
                    generator.Emit(OpCodes.Ceq);
                }
                else
                {
                    generator.DeclareLocal(propertyType);
                    generator.DeclareLocal(propertyType);
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                    generator.Emit(OpCodes.Stloc_0);
                    generator.Emit(OpCodes.Ldloca_S, 0);
                    generator.Emit(OpCodes.Ldloca_S, 1);
                    generator.Emit(OpCodes.Initobj, propertyType);
                    generator.Emit(OpCodes.Ldloc_1);
                    generator.Emit(OpCodes.Box, propertyType);
                    generator.Emit(OpCodes.Constrained, propertyType);
                    generator.Emit(OpCodes.Callvirt, ObjectEqual);
                }
            }
            else
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, property.GetMethod!);
                generator.Emit(OpCodes.Call, _isDefaultValueCache.DefaultMethod);
            }
        }

        generator.Emit(OpCodes.Ret);
    }

    private void GenerateScalarPropertyEqualIL(ILGenerator generator, PropertyInfo sourceProperty, PropertyInfo targetProperty)
    {
        var sourcePropertyType = sourceProperty.PropertyType;
        var targetPropertyType = targetProperty.PropertyType;
        var equalMethod = _areEqualCache.GetMethodFor(targetPropertyType);
        if (sourcePropertyType != targetPropertyType)
        {
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
            generator.Emit(OpCodes.Callvirt, _scalarPropertyConverterCache.CreateIfNotExist(sourcePropertyType, targetPropertyType));
            if (equalMethod == default && targetPropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, targetPropertyType);
            }
        }
        else
        {
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
            if (equalMethod == default && sourcePropertyType.IsValueType)
            {
                generator.Emit(OpCodes.Box, sourcePropertyType);
            }
        }

        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetProperty.GetMethod!);
        if (equalMethod == default && targetPropertyType.IsValueType)
        {
            generator.Emit(OpCodes.Box, targetPropertyType);
        }

        if (equalMethod != default)
        {
            generator.Emit(OpCodes.Call, equalMethod);
        }
        else
        {
            generator.Emit(OpCodes.Call, _areEqualCache.DefaultMethod);
        }

        generator.Emit(OpCodes.Ret);
    }
}

public interface IScalarTypeMethodCache
{
    MethodInfo DefaultMethod { get; }

    MethodInfo? GetMethodFor(Type type);
}

internal class ScalarTypeMethodCache : IScalarTypeMethodCache
{
    private readonly MethodInfo _generalMethod;

    private readonly Dictionary<Type, MethodInfo> _cache;

    public ScalarTypeMethodCache(Type type, string methodName, Type[] parameters)
    {
        _cache = type.GetMethods(BindingFlags.Public | BindingFlags.Static).ToDictionary(m => m.GetParameters()[0].ParameterType, m => m);
        _generalMethod = type.GetMethod(methodName, parameters)!;
    }

    public MethodInfo DefaultMethod => _generalMethod;

    public MethodInfo? GetMethodFor(Type type) => _cache.TryGetValue(type, out var result) ? result : default;
}

public static class ScalarTypeIsDefaultValueMethods
{
    public static bool IsDefaultValue(byte x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(byte? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(short x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(ushort x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(short? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(ushort? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(int x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(uint x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(int? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(uint? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(long x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(ulong x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(long? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(ulong? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(string? x)
    {
        return string.IsNullOrEmpty(x);
    }

    public static bool IsDefaultValue(byte[]? x)
    {
        return x == default || !x.Any();
    }

    public static bool IsDefaultValue(Guid x)
    {
        return x == default;
    }

    public static bool IsDefaultValue(Guid? x)
    {
        return !x.HasValue;
    }

    public static bool IsDefaultValue(object? x)
    {
        return x == default;
    }
}

public static class ScalarTypeEqualMethods
{
    public static bool AreEqual(byte x, byte y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(byte? x, byte? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(short x, short y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(ushort x, ushort y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(short? x, short? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(ushort? x, ushort? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(int x, int y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(uint x, uint y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(int? x, int? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(uint? x, uint? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(long x, long y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(ulong x, ulong y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(long? x, long? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(ulong? x, ulong? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(string? x, string? y)
    {
        return string.Equals(x, y);
    }

    public static bool AreEqual(byte[]? x, byte[]? y)
    {
        return x != default && y != default && Enumerable.SequenceEqual(x, y);
    }

    public static bool AreEqual(Guid x, Guid y)
    {
        return x != default && y != default && x == y;
    }

    public static bool AreEqual(Guid? x, Guid? y)
    {
        return x.HasValue && y.HasValue && x.Value == y.Value;
    }

    public static bool AreEqual(object? x, object? y)
    {
        return x != default && y != default && Equals(x, y);
    }
}
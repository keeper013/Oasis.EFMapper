namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;
using System.Reflection.Emit;

internal enum KeyType
{
    /// <summary>
    /// Id
    /// </summary>
    Id,

    /// <summary>
    /// Concurrenty Token
    /// </summary>
    ConcurrencyToken,
}

internal sealed class DynamicMethodBuilder
{
    private const char MapKeyPropertiesMethod = 'k';
    private const char CompareIdMethod = 'i';
    private const char CompareConcurrencyTokenMethod = 'o';
    private const char GetId = 'd';
    private const char IdEmpty = 'b';
    private const char ConcurrencyTokenEmpty = 'n';
    private const char BuildExistingTargetTracker = 'u';
    private const char StartTrackExistingTarget = 's';
    private const char Construct = 'c';

    private static readonly MethodInfo ObjectEqual = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) })!;
    private static readonly Type _delegateType = typeof(Delegate);
    private readonly GenericMapperMethodCache _scalarPropertyConverterCache = new (typeof(IScalarTypeConverter).GetMethods().First(m => string.Equals(m.Name, nameof(IScalarTypeConverter.Convert)) && m.IsGenericMethod));
    private readonly GenericMapperMethodCache _entityPropertyMapperCache = new (typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.MapEntityProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _entityListPropertyMapperCache = new (typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.MapListProperty), Utilities.PublicInstance)!);
    private readonly GenericMapperMethodCache _listTypeConstructorCache = new (typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.ConstructListType), Utilities.PublicInstance)!);
    private readonly IScalarTypeMethodCache _isDefaultValueCache = new ScalarTypeMethodCache(typeof(ScalarTypeIsDefaultValueMethods), nameof(ScalarTypeIsDefaultValueMethods.IsDefaultValue), new[] { typeof(object) });
    private readonly IScalarTypeMethodCache _areEqualCache = new ScalarTypeMethodCache(typeof(ScalarTypeEqualMethods), nameof(ScalarTypeEqualMethods.AreEqual), new[] { typeof(object), typeof(object) });
    private readonly TypeBuilder _typeBuilder;
    private readonly IMapperTypeValidator _scalarMapperTypeValidator;
    private readonly IMapperTypeValidator _entityMapperTypeValidator;
    private readonly IMapperTypeValidator _entityListMapperTypeValidator;

    public DynamicMethodBuilder(
        TypeBuilder typeBuilder,
        IMapperTypeValidator scalarMapperTypeValidator,
        IMapperTypeValidator entityMapperTypeValidator,
        IMapperTypeValidator entityListMapperTypeValidator)
    {
        _typeBuilder = typeBuilder;
        _scalarMapperTypeValidator = scalarMapperTypeValidator;
        _entityMapperTypeValidator = entityMapperTypeValidator;
        _entityListMapperTypeValidator = entityListMapperTypeValidator;
    }

    public Type Build()
    {
        return _typeBuilder.CreateType()!;
    }

    public MethodMetaData? BuildUpKeyPropertiesMapperMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceIdentityProperty,
        PropertyInfo? targetIdentityProperty,
        PropertyInfo? sourceConcurrencyTokenProperty,
        PropertyInfo? targetConcurrencyTokenProperty)
    {
        if (sourceIdentityProperty != default && targetIdentityProperty != default)
        {
            var methodName = BuildMapperMethodName(MapKeyPropertiesMethod, sourceType, targetType);
            var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(void));
            var generator = method.GetILGenerator();
            GenerateKeyPropertiesMappingCode(generator, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
            generator.Emit(OpCodes.Ret);
            return new MethodMetaData(typeof(Utilities.MapScalarProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
        }

        return null;
    }

    public MethodMetaData? BuildUpContentMappingMethod(
        Type sourceType,
        Type targetType,
        List<PropertyInfo> sourceProperties,
        List<PropertyInfo> targetProperties,
        IRecursiveRegister recursiveRegister,
        RecursiveRegisterContext context)
    {
        var mappedScalarProperties = ExtractScalarProperties(sourceProperties, targetProperties);
        var generateScalarPropertyMappingCode = mappedScalarProperties.Any();
        var mappedEntityProperties = ExtractEntityProperties(sourceProperties, targetProperties, recursiveRegister, context);
        var generateEntityPropertyMappingCode = mappedEntityProperties.Any();
        var mappedEntityListProperties = ExtractEntityListProperties(sourceProperties, targetProperties, recursiveRegister, context);

        var generateEntityListPropertyMappingCode = mappedEntityListProperties.Any();

        if (generateScalarPropertyMappingCode || generateEntityPropertyMappingCode || generateEntityListPropertyMappingCode)
        {
            var methodName = BuildMapperMethodName(sourceType, targetType);
            var method = BuildMethod(
                methodName,
                new[] { sourceType, targetType, typeof(IScalarTypeConverter), typeof(IRecursiveMapper<int>), typeof(IExistingTargetTracker), typeof(INewTargetTracker<int>), typeof(bool?) },
                typeof(void));
            var generator = method.GetILGenerator();

            if (generateScalarPropertyMappingCode)
            {
                GenerateScalarPropertiesMappingCode(generator, mappedScalarProperties);
            }

            if (generateEntityPropertyMappingCode)
            {
                GenerateEntityPropertiesMappingCode(generator, mappedEntityProperties);
            }

            if (generateEntityListPropertyMappingCode)
            {
                GenerateEntityListPropertiesMappingCode(generator, mappedEntityListProperties);
            }

            generator.Emit(OpCodes.Ret);
            return new MethodMetaData(typeof(Utilities.MapProperties<,,>).MakeGenericType(sourceType, targetType, typeof(int)), method.Name);
        }

        return null;
    }

    public MethodMetaData BuildUpKeyEqualComparerMethod(
        KeyType keyType,
        Type sourceType,
        Type targetType,
        PropertyInfo sourceKeyProperty,
        PropertyInfo targetKeyProperty)
    {
        return BuildUpKeyEqualComparerMethod(sourceType, targetType, sourceKeyProperty, targetKeyProperty, keyType == KeyType.Id ? CompareIdMethod : CompareConcurrencyTokenMethod);
    }

    public MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty)
    {
        return BuildUpGetKeyMethod(type, identityProperty, GetId);
    }

    public MethodMetaData BuildUpKeyIsEmptyMethod(KeyType keyType, Type type, PropertyInfo identityProperty)
    {
        return BuildUpKeyIsEmptyMethod(type, identityProperty, keyType == KeyType.Id ? IdEmpty : ConcurrencyTokenEmpty);
    }

    public MethodMetaData BuildUpBuildExistingTargetTrackerMethod(Type targetType, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(BuildExistingTargetTracker, targetType);
        var identityType = identityProperty.PropertyType;
        var classType = typeof(ExistingTargetTracker<>).MakeGenericType(identityType);
        var constructor = classType.GetConstructor(new Type[] { _delegateType })!;
        var method = BuildMethod(methodName, new[] { _delegateType }, typeof(IExistingTargetTracker));
        var generator = method.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Newobj, constructor);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.BuildExistingTargetTracker), method.Name);
    }

    public MethodMetaData BuildUpStartTrackExistingTargetMethod(Type targetType, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(StartTrackExistingTarget, targetType);
        var identityType = identityProperty.PropertyType;
        var keySetType = typeof(ISet<>).MakeGenericType(identityType);
        var addMethod = keySetType.GetMethod("Add")!;
        var method = BuildMethod(methodName, new[] { keySetType, targetType }, typeof(bool));
        var generator = method.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
        generator.Emit(OpCodes.Callvirt, addMethod);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.StartTrackingNewTarget<,>).MakeGenericType(targetType, identityType), method.Name);
    }

    public MethodMetaData BuildUpConstructorMethod(Type type)
    {
        var methodName = BuildMethodName(Construct, type);
        var method = BuildMethod(methodName, Array.Empty<Type>(), type);
        var generator = method.GetILGenerator();
        var constructorInfo = type.GetConstructor(Utilities.PublicInstance, null, Array.Empty<Type>(), null);
        if (constructorInfo == null)
        {
            throw new UnconstructableTypeException(type);
        }

        generator.Emit(OpCodes.Newobj, constructorInfo);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Func<>).MakeGenericType(type), method.Name);
    }

    private static string GetTypeName(Type type)
    {
        return $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
    }

    private static string BuildMapperMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{GetTypeName(sourceType)}__MapTo__{GetTypeName(targetType)}";
    }

    private static string BuildMethodName(char prefix, Type entityType)
    {
        return $"_{prefix}__{GetTypeName(entityType)}";
    }

    private static string BuildMapperMethodName(Type sourceType, Type targetType)
    {
        return $"_{GetTypeName(sourceType)}__MapTo__{GetTypeName(targetType)}";
    }

    private static string BuildPropertyCompareMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{GetTypeName(sourceType)}__CompareTo__{GetTypeName(targetType)}";
    }

    private void GenerateKeyPropertiesMappingCode(
        ILGenerator generator,
        PropertyInfo sourceIdentityProperty,
        PropertyInfo targetIdentityProperty,
        PropertyInfo? sourceConcurrencyTokenProperty,
        PropertyInfo? targetConcurrencyTokenProperty)
    {
        GenerateScalarPropertyValueAssignmentIL(generator, sourceIdentityProperty!, targetIdentityProperty!);

        if (sourceConcurrencyTokenProperty != default && targetConcurrencyTokenProperty != default)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, sourceConcurrencyTokenProperty!, targetConcurrencyTokenProperty!);
        }
    }

    private IList<(PropertyInfo, PropertyInfo)> ExtractScalarProperties(IList<PropertyInfo> sourceProperties, IList<PropertyInfo> targetProperties)
    {
        var sourceScalarProperties = sourceProperties.Where(p => _scalarMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetScalarProperties = targetProperties.Where(p => _scalarMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true)).ToDictionary(p => p.Name, p => p);

        var matchedProperties = new List<(PropertyInfo, PropertyInfo)>(sourceScalarProperties.Count());
        foreach (var sourceProperty in sourceScalarProperties)
        {
            if (targetScalarProperties.TryGetValue(sourceProperty.Name, out var targetProperty)
                && (sourceProperty.PropertyType == targetProperty.PropertyType || _scalarMapperTypeValidator.CanConvert(sourceProperty.PropertyType, targetProperty.PropertyType)))
            {
                matchedProperties.Add((sourceProperty, targetProperty));
            }
        }

        foreach (var match in matchedProperties)
        {
            sourceProperties.Remove(match.Item1);
            targetProperties.Remove(match.Item2);
        }

        return matchedProperties;
    }

    private void GenerateScalarPropertiesMappingCode(
        ILGenerator generator,
        IList<(PropertyInfo, PropertyInfo)> matchedProperties)
    {
        foreach (var match in matchedProperties)
        {
            GenerateScalarPropertyValueAssignmentIL(generator, match.Item1, match.Item2);
        }
    }

    private IList<(PropertyInfo, Type, PropertyInfo, Type)> ExtractEntityProperties(
        IList<PropertyInfo> sourceProperties,
        IList<PropertyInfo> targetProperties,
        IRecursiveRegister recursiveRegister,
        RecursiveRegisterContext context)
    {
        var sourceEntityProperties = sourceProperties.Where(p => _entityMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetEntityProperties = targetProperties.Where(p => _entityMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true)).ToDictionary(p => p.Name, p => p);

        var matchedProperties = new List<(PropertyInfo, Type, PropertyInfo, Type)>(sourceEntityProperties.Count());
        foreach (var sourceProperty in sourceEntityProperties)
        {
            if (targetEntityProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourcePropertyType = sourceProperty.PropertyType;
                var targetPropertyType = targetProperty.PropertyType;

                // cascading mapper creation: if entity mapper doesn't exist, create it
                context.RegisterIf(recursiveRegister, sourcePropertyType, targetPropertyType, _entityMapperTypeValidator.CanConvert(sourcePropertyType, targetPropertyType));
                matchedProperties.Add((sourceProperty, sourcePropertyType, targetProperty, targetPropertyType));
            }
        }

        foreach (var match in matchedProperties)
        {
            sourceProperties.Remove(match.Item1);
            targetProperties.Remove(match.Item3);
        }

        return matchedProperties;
    }

    private void GenerateEntityPropertiesMappingCode(
        ILGenerator generator,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties)
    {
        foreach (var matched in matchedProperties)
        {
            // now it's made sure that mapper between entities exists, emit the entity property mapping code
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, matched.Item1.GetMethod!);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, matched.Item3.GetMethod!);
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Ldarg, 5);
            generator.Emit(OpCodes.Ldstr, matched.Item3.Name);
            generator.Emit(OpCodes.Ldarg, 6);
            generator.Emit(OpCodes.Callvirt, _entityPropertyMapperCache.CreateIfNotExist(matched.Item2, matched.Item4));
            generator.Emit(OpCodes.Callvirt, matched.Item3.SetMethod!);
        }
    }

    private IList<(PropertyInfo, Type, PropertyInfo, Type)> ExtractEntityListProperties(
        IList<PropertyInfo> sourceProperties,
        IList<PropertyInfo> targetProperties,
        IRecursiveRegister recursiveRegister,
        RecursiveRegisterContext context)
    {
        var sourceEntityListProperties = sourceProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetEntityListProperties = targetProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true)).ToDictionary(p => p.Name, p => p);
        var matchedProperties = new List<(PropertyInfo, Type, PropertyInfo, Type)>(sourceEntityListProperties.Count());
        foreach (var sourceProperty in sourceEntityListProperties)
        {
            if (targetEntityListProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var targetType = targetProperty.PropertyType;
                var sourceItemType = sourceProperty.PropertyType.GetListItemType()!;
                var targetItemType = targetType.GetListItemType()!;

                // cascading mapper creation: if list item mapper doesn't exist, create it
                context.RegisterIf(recursiveRegister, sourceItemType, targetItemType, _entityListMapperTypeValidator.CanConvert(sourceItemType, targetItemType));
                recursiveRegister.RegisterEntityListDefaultConstructorMethod(targetType);
                matchedProperties.Add((sourceProperty, sourceItemType, targetProperty, targetItemType));
            }
        }

        return matchedProperties;
    }

    private void GenerateEntityListPropertiesMappingCode(
        ILGenerator generator,
        IList<(PropertyInfo, Type, PropertyInfo, Type)> matchedProperties)
    {
        foreach (var match in matchedProperties)
        {
            // now it's made sure that mapper between list items exists, emit the list property mapping code
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, match.Item3.GetMethod!);
            var jumpLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brtrue_S, jumpLabel);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Callvirt, _listTypeConstructorCache.CreateIfNotExist(match.Item3.PropertyType, match.Item4));
            generator.Emit(OpCodes.Callvirt, match.Item3.SetMethod!);
            generator.MarkLabel(jumpLabel);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, match.Item1.GetMethod!);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, match.Item3.GetMethod!);
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Ldarg, 5);
            generator.Emit(OpCodes.Ldstr, match.Item3.Name);
            generator.Emit(OpCodes.Ldarg, 6);
            generator.Emit(OpCodes.Callvirt, _entityListPropertyMapperCache.CreateIfNotExist(match.Item2, match.Item4));
        }
    }

    private MethodMetaData BuildUpKeyEqualComparerMethod(
        Type sourceType,
        Type targetType,
        PropertyInfo sourceProperty,
        PropertyInfo targetProperty,
        char prefix)
    {
        var methodName = BuildPropertyCompareMethodName(prefix, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IScalarTypeConverter) }, typeof(bool));
        var generator = method.GetILGenerator();
        GenerateScalarPropertyEqualIL(generator, sourceProperty, targetProperty);
        return new MethodMetaData(typeof(Utilities.ScalarPropertiesAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    private MethodMetaData BuildUpGetKeyMethod(Type type, PropertyInfo keyProperty, char prefix)
    {
        var methodName = BuildMethodName(prefix, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(object));
        var generator = method.GetILGenerator();

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, keyProperty.GetMethod!);
        var keyPropertyType = keyProperty.PropertyType;
        if (keyPropertyType.IsValueType)
        {
            generator.Emit(OpCodes.Box, keyPropertyType);
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetScalarProperty<>).MakeGenericType(type), method.Name);
    }

    private MethodMetaData BuildUpKeyIsEmptyMethod(Type type, PropertyInfo keyProperty, char prefix)
    {
        var methodName = BuildMethodName(prefix, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(bool));
        var generator = method.GetILGenerator();
        GenerateScalarPropertyEmptyIL(generator, keyProperty);
        return new MethodMetaData(typeof(Utilities.ScalarPropertyIsEmpty<>).MakeGenericType(type), method.Name);
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
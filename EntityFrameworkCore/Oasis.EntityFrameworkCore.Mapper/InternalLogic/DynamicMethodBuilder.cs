namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System;
using System.Linq.Expressions;
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

internal enum TypeEqualCategory
{
    /// <summary>
    /// Types overrides equal operator
    /// </summary>
    OpEquality,

    /// <summary>
    /// Types that are primitive or enum or class
    /// </summary>
    PrimitiveEnumClass,

    /// <summary>
    /// Nullable structs that overrides equal operator
    /// </summary>
    NullableOpEquality,

    /// <summary>
    /// Nullable primitive or enum
    /// </summary>
    NullablePrimitiveEnum,

    /// <summary>
    /// Equals as objects
    /// </summary>
    ObjectEquals,
}

internal enum TypeIsDefaultCategory
{
    /// <summary>
    /// Type is class, call ldnull and ceq
    /// </summary>
    Class,

    /// <summary>
    /// Type is date or guid
    /// </summary>
    Struct,

    /// <summary>
    /// Type is decimal
    /// </summary>
    Decimal,

    /// <summary>
    /// Type is long/ulong
    /// </summary>
    Long,

    /// <summary>
    /// Type is double
    /// </summary>
    Double,

    /// <summary>
    /// Type is float
    /// </summary>
    Float,

    /// <summary>
    /// Type is int/uint/short/ushort/byte
    /// </summary>
    IntAndBelow,
}

internal sealed class DynamicMethodBuilder
{
    private const char CompareConcurrencyTokenMethod = 'c';
    private const char CompareIdMethod = 'o';
    private const char ConcurrencyTokenEmpty = 'n';
    private const char Construct = 's';
    private const char GetId = 'g';
    private const char IdEmpty = 'i';
    private const char MapKeyPropertiesMethod = 'm';
    private const char SourceIdForTarget = 'u';
    private const char SourceIdEqualsTargetId = 'r';
    private const char SourceIdListContainsTargetId = 'e';
    private const char TargetByIdTrackerFind = 't';
    private const char TargetByIdTrackerTrack = 'a';

    private const string DictionaryItemMethodName = "get_Item";
    private const string EqualOperatorMethodName = "op_Equality";
    private const string GetValueOrDefaultMethodName = "GetValueOrDefault";
    private const string HasValuePropertyName = "HasValue";
    private const string ValuePropertyName = "Value";

    private static readonly MethodInfo ObjectEqual = typeof(object).GetMethod(nameof(object.Equals), new[] { typeof(object) })!;
    private static readonly Type DelegateType = typeof(Delegate);
    private static readonly Type ByteArrayType = typeof(byte[]);
    private static readonly Type ByteType = typeof(byte);
    private static readonly MethodInfo TypeOfMethod = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), Utilities.PublicStatic)!;
    private static readonly MethodInfo ScalarConvertorOuterGetItem = typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ScalarConvertorInnerGetItem = typeof(IReadOnlyDictionary<Type, Delegate>).GetMethod(DictionaryItemMethodName, Utilities.PublicInstance)!;
    private static readonly MethodInfo ByteArraySequenceEqual = typeof(Enumerable).GetMethods(Utilities.PublicStatic).Single(m => string.Equals(m.Name, nameof(Enumerable.SequenceEqual)) && m.GetParameters().Length == 2).MakeGenericMethod(ByteType);
    private static readonly MethodInfo EntityPropertyMapper = typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.MapEntityProperty), Utilities.PublicInstance)!;
    private static readonly MethodInfo EntityListPropertyMapper = typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.MapListProperty), Utilities.PublicInstance)!;
    private static readonly MethodInfo ListTypeConstructor = typeof(IRecursiveMapper<int>).GetMethod(nameof(IRecursiveMapper<int>.ConstructListType), Utilities.PublicInstance)!;
    private static readonly ConstructorInfo InitializeOnlyPropertyExceptionContructor = typeof(InitializeOnlyPropertyException).GetConstructor(new[] { typeof(Type), typeof(string) })!;
    private static readonly FieldInfo DecimalZeroField = typeof(decimal).GetField(nameof(decimal.Zero), Utilities.PublicStatic)!;
    private static readonly MethodInfo DecimalOpEquality = typeof(decimal).GetMethod(nameof(EqualOperatorMethodName), Utilities.PublicStatic)!;
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
            var methodName = BuildMethodName(MapKeyPropertiesMethod, sourceType, targetType);
            var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>), typeof(bool) }, typeof(void));
            var generator = method.GetILGenerator();
            GenerateKeyPropertiesMappingCode(generator, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
            generator.Emit(OpCodes.Ret);
            return new MethodMetaData(typeof(Utilities.MapKeyProperties<,>).MakeGenericType(sourceType, targetType), method.Name);
        }

        return null;
    }

    public MethodMetaData? BuildUpContentMappingMethod(
        Type sourceType,
        Type targetType,
        List<PropertyInfo> sourceProperties,
        List<PropertyInfo> targetProperties,
        IRecursiveRegister recursiveRegister,
        IRecursiveRegisterContext context)
    {
        var mappedScalarProperties = ExtractScalarProperties(sourceProperties, targetProperties);
        var generateScalarPropertyMappingCode = mappedScalarProperties.Any();
        var mappedEntityProperties = ExtractEntityProperties(sourceProperties, targetProperties, recursiveRegister, context);
        var generateEntityPropertyMappingCode = mappedEntityProperties.Any();
        var mappedEntityListProperties = ExtractEntityListProperties(sourceProperties, targetProperties, recursiveRegister, context);

        var generateEntityListPropertyMappingCode = mappedEntityListProperties.Any();

        if (generateScalarPropertyMappingCode || generateEntityPropertyMappingCode || generateEntityListPropertyMappingCode)
        {
            var methodName = BuildMethodName(sourceType, targetType);
            var method = BuildMethod(
                methodName,
                new[] { sourceType, targetType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>), typeof(IRecursiveMapper<int>), typeof(IRecursiveMappingContext) },
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
        var methodName = BuildPropertyCompareMethodName(keyType == KeyType.Id ? CompareIdMethod : CompareConcurrencyTokenMethod, sourceType, targetType);
        var method = BuildMethod(methodName, new[] { sourceType, targetType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>) }, typeof(bool));
        var generator = method.GetILGenerator();

        var sourcePropertyType = sourceKeyProperty.PropertyType;
        var targetPropertyType = targetKeyProperty.PropertyType;
        var equalCategory = GetTypeEqualityCategory(targetPropertyType);
        var targetTypeIsNullable = equalCategory == TypeEqualCategory.NullableOpEquality || equalCategory == TypeEqualCategory.NullablePrimitiveEnum;
        var needToBox = equalCategory == TypeEqualCategory.ObjectEquals && targetPropertyType.IsValueType;
        var typeIsByteArray = targetPropertyType == ByteArrayType;
        LocalBuilder? sourceLocal = null;
        LocalBuilder? targetLocal = null;
        Label jumpLabel = default;
        if (targetTypeIsNullable)
        {
            sourceLocal = generator.DeclareLocal(targetPropertyType);
            targetLocal = generator.DeclareLocal(targetPropertyType);
        }
        else if (typeIsByteArray)
        {
            sourceLocal = generator.DeclareLocal(targetPropertyType);
        }

        GenerateScalarPropertyConvertingCode(
            generator,
            sourcePropertyType,
            targetPropertyType,
            g => g.Emit(OpCodes.Ldarg_2),
            g =>
            {
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Callvirt, sourceKeyProperty.GetMethod!);
                if (targetTypeIsNullable)
                {
                    g.Emit(OpCodes.Stloc_0);
                }
            });

        if (needToBox)
        {
            generator.Emit(OpCodes.Box, targetPropertyType);
        }
        else if (typeIsByteArray)
        {
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloc_0);
            jumpLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        }

        generator.Emit(OpCodes.Ldarg_1);
        generator.Emit(OpCodes.Callvirt, targetKeyProperty.GetMethod!);
        if (targetTypeIsNullable)
        {
            generator.Emit(OpCodes.Stloc_1);
        }

        if (needToBox)
        {
            generator.Emit(OpCodes.Box, targetPropertyType);
        }
        else if (typeIsByteArray)
        {
            generator.Emit(OpCodes.Brfalse_S, jumpLabel);
        }

        if (!typeIsByteArray)
        {
            GenerateEqualCode(generator, targetPropertyType, equalCategory, sourceLocal, targetLocal);
        }
        else
        {
            generator.Emit(OpCodes.Ldloc_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, targetKeyProperty.GetMethod!);
            generator.Emit(OpCodes.Call, ByteArraySequenceEqual);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(jumpLabel);
            generator.Emit(OpCodes.Ldc_I4_0);
        }

        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Utilities.ScalarPropertiesAreEqual<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpKeyIsEmptyMethod(KeyType keyType, Type type, PropertyInfo identityProperty)
    {
        var methodName = BuildMethodName(keyType == KeyType.Id ? IdEmpty : ConcurrencyTokenEmpty, type);
        var method = BuildMethod(methodName, new[] { type }, typeof(bool));
        var generator = method.GetILGenerator();

        var propertyType = identityProperty.PropertyType;
        var (category, isNullable) = GetTypeIsDefaultCategory(propertyType);

        if (isNullable)
        {
            var propertyLocal = generator.DeclareLocal(propertyType);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
            generator.Emit(OpCodes.Stloc_0);
            generator.Emit(OpCodes.Ldloca_S, propertyLocal);
            generator.Emit(OpCodes.Call, propertyType.GetProperty(HasValuePropertyName, Utilities.PublicInstance)!.GetMethod!);
            if (category == TypeIsDefaultCategory.Struct)
            {
                generator.Emit(OpCodes.Ldc_I4_0);
                generator.Emit(OpCodes.Ceq);
            }
            else
            {
                var jumpLabel = generator.DefineLabel();
                generator.Emit(OpCodes.Brfalse_S, jumpLabel);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);
                generator.Emit(OpCodes.Stloc_0);
                generator.Emit(OpCodes.Ldloca_S, propertyLocal);
                generator.Emit(OpCodes.Call, propertyType.GetProperty(ValuePropertyName, Utilities.PublicInstance)!.GetMethod!);
                if (category == TypeIsDefaultCategory.Decimal)
                {
                    generator.Emit(OpCodes.Ldsfld, DecimalZeroField);
                    generator.Emit(OpCodes.Call, DecimalOpEquality);
                }
                else
                {
                    switch (category)
                    {
                        case TypeIsDefaultCategory.Long:
                            generator.Emit(OpCodes.Ldc_I4_0);
                            generator.Emit(OpCodes.Conv_I8);
                            break;
                        case TypeIsDefaultCategory.Double:
                            generator.Emit(OpCodes.Ldc_R8, 0);
                            break;
                        case TypeIsDefaultCategory.Float:
                            generator.Emit(OpCodes.Ldc_R4, 0);
                            break;
                        default:
                            generator.Emit(OpCodes.Ldc_I4_0);
                            break;
                    }

                    generator.Emit(OpCodes.Ceq);
                    generator.Emit(OpCodes.Ret);
                    generator.MarkLabel(jumpLabel);
                    generator.Emit(OpCodes.Ldc_I4_1);
                }
            }
        }
        else
        {
            LocalBuilder? structLocal = null!;
            if (category == TypeIsDefaultCategory.Struct)
            {
                structLocal = generator.DeclareLocal(propertyType);
            }

            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, identityProperty.GetMethod!);

            switch (category)
            {
                case TypeIsDefaultCategory.Class:
                    generator.Emit(OpCodes.Ldnull);
                    generator.Emit(OpCodes.Ceq);
                    break;
                case TypeIsDefaultCategory.Decimal:
                    generator.Emit(OpCodes.Ldsfld, DecimalZeroField);
                    generator.Emit(OpCodes.Call, DecimalOpEquality);
                    break;
                case TypeIsDefaultCategory.Double:
                    generator.Emit(OpCodes.Ldc_R8, 0);
                    generator.Emit(OpCodes.Ceq);
                    break;
                case TypeIsDefaultCategory.Float:
                    generator.Emit(OpCodes.Ldc_R4, 0);
                    generator.Emit(OpCodes.Ceq);
                    break;
                case TypeIsDefaultCategory.IntAndBelow:
                    generator.Emit(OpCodes.Ldc_I4_0, 0);
                    generator.Emit(OpCodes.Ceq);
                    break;
                case TypeIsDefaultCategory.Long:
                    generator.Emit(OpCodes.Ldc_I4_0, 0);
                    generator.Emit(OpCodes.Conv_I8);
                    generator.Emit(OpCodes.Ceq);
                    break;
                default:
                    generator.Emit(OpCodes.Ldloca_S, structLocal);
                    generator.Emit(OpCodes.Initobj, propertyType);
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Call, propertyType.GetMethod(EqualOperatorMethodName, Utilities.PublicStatic)!);
                    break;
            }
        }

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.ScalarPropertyIsEmpty<>).MakeGenericType(type), method.Name);
    }

    public MethodMetaData BuildUpSourceIdListContainsTargetIdMethod(Type sourceType, PropertyInfo sourceIdentityProperty, Type targetType, PropertyInfo targetIdentityProperty)
    {
        var methodName = BuildMethodName(SourceIdListContainsTargetId, sourceType, targetType);
        var sourceListType = typeof(List<>).MakeGenericType(sourceType);
        var method = BuildMethod(
            methodName,
            new[] { sourceListType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>) },
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(targetType, typeof(bool))));
        var generator = method.GetILGenerator();
        var sourceIdentityType = sourceIdentityProperty.PropertyType;
        var targetIdentityType = targetIdentityProperty.PropertyType;
        var needToConvert = sourceIdentityType != targetIdentityType;
        var identityListType = typeof(List<>).MakeGenericType(targetIdentityType);
        var countMethod = sourceListType.GetProperty(nameof(List<int>.Count), Utilities.PublicInstance)!.GetMethod!;
        var identityListConstructor = identityListType.GetConstructor(new[] { typeof(int) })!;
        var getEnumeratorMethod = sourceListType.GetMethod(nameof(List<int>.GetEnumerator), Utilities.PublicInstance)!;
        var enumeratorType = typeof(List<>.Enumerator).MakeGenericType(sourceType);
        var getCurrentMethod = enumeratorType.GetProperty(nameof(List<int>.Enumerator.Current), Utilities.PublicInstance)!.GetMethod!;
        var addMethod = identityListType.GetMethod(nameof(List<int>.Add), Utilities.PublicInstance)!;
        var moveNextMethod = enumeratorType.GetMethod(nameof(List<int>.Enumerator.MoveNext), Utilities.PublicInstance)!;
        var disposeMethod = enumeratorType.GetMethod(nameof(List<int>.Enumerator.Dispose), Utilities.PublicInstance)!;
        var makeContainsIdExpressionMethod =
            typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.MakeContainsIdExpression), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(targetType, targetIdentityType);

        var listLocalVariable = generator.DeclareLocal(identityListType);
        var enumeratorLocalVariable = generator.DeclareLocal(enumeratorType);
        var sourceLocalVaraible = generator.DeclareLocal(sourceType);

        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, countMethod);
        generator.Emit(OpCodes.Newobj, identityListConstructor);
        generator.Emit(OpCodes.Stloc, listLocalVariable);
        generator.Emit(OpCodes.Ldarg_0);
        generator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        generator.Emit(OpCodes.Stloc, enumeratorLocalVariable);

        generator.BeginExceptionBlock();
        var startingJumpLabel = generator.DefineLabel();
        var loopJumpLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Br_S, startingJumpLabel);
        generator.MarkLabel(loopJumpLabel);
        generator.Emit(OpCodes.Ldloca_S, enumeratorLocalVariable);
        generator.Emit(OpCodes.Call, getCurrentMethod);
        generator.Emit(OpCodes.Stloc, sourceLocalVaraible);
        generator.Emit(OpCodes.Ldloc, listLocalVariable);

        GenerateScalarPropertyConvertingCode(
            generator,
            sourceIdentityType,
            targetIdentityType,
            g => g.Emit(OpCodes.Ldarg_1),
            g =>
            {
                generator.Emit(OpCodes.Ldloc, sourceLocalVaraible);
                generator.Emit(OpCodes.Callvirt, sourceIdentityProperty.GetMethod!);
            });

        generator.Emit(OpCodes.Callvirt, addMethod);
        generator.MarkLabel(startingJumpLabel);
        generator.Emit(OpCodes.Ldloca_S, enumeratorLocalVariable);
        generator.Emit(OpCodes.Call, moveNextMethod);
        generator.Emit(OpCodes.Brtrue_S, loopJumpLabel);
        var leaveLabel = generator.DefineLabel();
        generator.Emit(OpCodes.Leave_S, leaveLabel);

        generator.BeginFinallyBlock();
        generator.Emit(OpCodes.Ldloca_S, enumeratorLocalVariable);
        generator.Emit(OpCodes.Constrained, enumeratorType);
        generator.Emit(OpCodes.Callvirt, disposeMethod);
        generator.Emit(OpCodes.Endfinally);
        generator.EndExceptionBlock();

        generator.MarkLabel(leaveLabel);
        generator.Emit(OpCodes.Ldloc, listLocalVariable);
        generator.Emit(OpCodes.Ldstr, targetIdentityProperty.Name);
        generator.Emit(OpCodes.Call, makeContainsIdExpressionMethod);
        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetSourceIdListContainsTargetId<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpSourceIdEqualsTargetIdMethod(Type sourceType, PropertyInfo sourceIdentityProperty, Type targetType, PropertyInfo targetIdentityProperty)
    {
        var methodName = BuildMethodName(SourceIdEqualsTargetId, sourceType, targetType);
        var method = BuildMethod(
            methodName,
            new[] { sourceType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>) },
            typeof(Expression<>).MakeGenericType(typeof(Func<,>).MakeGenericType(targetType, typeof(bool))));
        var generator = method.GetILGenerator();
        var sourceIdentityType = sourceIdentityProperty.PropertyType;
        var targetIdentityType = targetIdentityProperty.PropertyType;
        var needToConvert = sourceIdentityType != targetIdentityType;
        var makeIdEqualsExpression =
            typeof(ExpressionUtilities).GetMethod(nameof(ExpressionUtilities.MakeIdEqualsExpression), BindingFlags.Static | BindingFlags.Public)!
            .MakeGenericMethod(targetType, targetIdentityType);

        GenerateScalarPropertyConvertingCode(
            generator,
            sourceIdentityType,
            targetIdentityType,
            g => g.Emit(OpCodes.Ldarg_1),
            g =>
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceIdentityProperty.GetMethod!);
            });

        generator.Emit(OpCodes.Ldtoken, targetIdentityType);
        generator.Emit(OpCodes.Call, TypeOfMethod);
        generator.Emit(OpCodes.Ldstr, targetIdentityProperty.Name);
        generator.Emit(OpCodes.Call, makeIdEqualsExpression);
        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.GetSourceIdEqualsTargetId<,>).MakeGenericType(sourceType, targetType), method.Name);
    }

    public MethodMetaData BuildUpTargetByIdTrackerFindMethod(Type sourceType, Type targetType, PropertyInfo sourceIdentityProperty, PropertyInfo targetIdentityProperty)
    {
        var methodName = BuildMethodName(TargetByIdTrackerFind, sourceType, targetType);
        var objectType = typeof(object);
        var sourceIdentityType = sourceIdentityProperty.PropertyType;
        var targetIdentityType = targetIdentityProperty.PropertyType;
        var sourceIdentityUnderlyingType = Nullable.GetUnderlyingType(sourceIdentityType);
        var targetIdentityUnderlyingType = Nullable.GetUnderlyingType(targetIdentityType);
        var sourceIdentityTypeIsTargetIdentityUnderlying = sourceIdentityType == targetIdentityUnderlyingType;
        var needTargetIdentityLocalVariable = targetIdentityUnderlyingType != null && !sourceIdentityTypeIsTargetIdentityUnderlying;
        var needToConvert = sourceIdentityType != targetIdentityType && !sourceIdentityTypeIsTargetIdentityUnderlying;
        var targetIdentityKeyType = targetIdentityUnderlyingType ?? targetIdentityType;
        var tryGetValueMethod = typeof(Dictionary<,>).MakeGenericType(targetIdentityKeyType, objectType).GetMethod(nameof(Dictionary<int, object>.TryGetValue), Utilities.PublicInstance)!;
        var method = BuildMethod(
            methodName,
            new[] { typeof(Dictionary<,>).MakeGenericType(targetIdentityKeyType, objectType), sourceType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>) },
            targetType);
        var generator = method.GetILGenerator();

        var targetObjLocalVariable = generator.DeclareLocal(objectType);
        LocalBuilder? targetIdentityLocalVariable = null!;
        if (needTargetIdentityLocalVariable)
        {
            targetIdentityLocalVariable = generator.DeclareLocal(targetIdentityType);
        }

        generator.Emit(OpCodes.Ldarg_0);

        GenerateScalarPropertyConvertingCode(
            generator,
            sourceIdentityType,
            targetIdentityType,
            g => g.Emit(OpCodes.Ldarg_2),
            g =>
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, sourceIdentityProperty.GetMethod!);
            });

        if (needTargetIdentityLocalVariable)
        {
            generator.Emit(OpCodes.Stloc, targetIdentityLocalVariable);
            generator.Emit(OpCodes.Ldloca_S, targetIdentityLocalVariable);
            generator.Emit(OpCodes.Call, targetIdentityType.GetProperty(nameof(Nullable<int>.Value), Utilities.PublicInstance)!.GetMethod!);
        }

        generator.Emit(OpCodes.Ldloca_S, targetObjLocalVariable);
        generator.Emit(OpCodes.Callvirt, tryGetValueMethod);
        var jumpLabel1 = generator.DefineLabel();
        generator.Emit(OpCodes.Brtrue_S, jumpLabel1);
        generator.Emit(OpCodes.Ldnull);
        var jumpLabel2 = generator.DefineLabel();
        generator.Emit(OpCodes.Br_S, jumpLabel2);
        generator.MarkLabel(jumpLabel1);
        generator.Emit(OpCodes.Ldloc, targetObjLocalVariable);
        generator.MarkLabel(jumpLabel2);
        generator.Emit(OpCodes.Castclass, targetType);

        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.EntityTrackerFindById<,,>).MakeGenericType(sourceType, targetType, targetIdentityKeyType), method.Name);
    }

    public MethodMetaData BuildUpTargetByIdTrackerTrackMethod(Type sourceType, Type targetType, PropertyInfo sourceIdentityProperty, PropertyInfo targetIdentityProperty)
    {
        var methodName = BuildMethodName(TargetByIdTrackerTrack, sourceType, targetType);
        var objectType = typeof(object);
        var sourceIdentityType = sourceIdentityProperty.PropertyType;
        var targetIdentityType = targetIdentityProperty.PropertyType;
        var sourceIdentityUnderlyingType = Nullable.GetUnderlyingType(sourceIdentityType);
        var targetIdentityUnderlyingType = Nullable.GetUnderlyingType(targetIdentityType);
        var sourceIdentityTypeIsTargetIdentityUnderlying = sourceIdentityType == targetIdentityUnderlyingType;
        var needTargetIdentityLocalVariable = targetIdentityUnderlyingType != null && !sourceIdentityTypeIsTargetIdentityUnderlying;
        var needToConvert = sourceIdentityType != targetIdentityType && !sourceIdentityTypeIsTargetIdentityUnderlying;
        var targetIdentityKeyType = targetIdentityUnderlyingType ?? targetIdentityType;
        var addMethod = typeof(Dictionary<,>).MakeGenericType(targetIdentityKeyType, objectType).GetMethod(nameof(Dictionary<int, object>.Add), Utilities.PublicInstance)!;
        var method = BuildMethod(
            methodName,
            new[] { typeof(Dictionary<,>).MakeGenericType(targetIdentityKeyType, objectType), sourceType, targetType, typeof(IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>>) },
            typeof(void));
        var generator = method.GetILGenerator();

        LocalBuilder targetIdentityLocalVariable = null!;
        if (needTargetIdentityLocalVariable)
        {
            targetIdentityLocalVariable = generator.DeclareLocal(targetIdentityType);
        }

        generator.Emit(OpCodes.Ldarg_0);
        GenerateScalarPropertyConvertingCode(
            generator,
            sourceIdentityType,
            targetIdentityType,
            g => g.Emit(OpCodes.Ldarg_3),
            g =>
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Callvirt, sourceIdentityProperty.GetMethod!);
            });

        if (needTargetIdentityLocalVariable)
        {
            generator.Emit(OpCodes.Stloc, targetIdentityLocalVariable);
            generator.Emit(OpCodes.Ldloca_S, targetIdentityLocalVariable);
            generator.Emit(OpCodes.Call, targetIdentityType.GetProperty(nameof(Nullable<int>.Value), Utilities.PublicInstance)!.GetMethod!);
        }

        generator.Emit(OpCodes.Ldarg_2);
        generator.Emit(OpCodes.Callvirt, addMethod);
        generator.Emit(OpCodes.Ret);

        return new MethodMetaData(typeof(Utilities.EntityTrackerTrackById<,,>).MakeGenericType(sourceType, targetType, targetIdentityKeyType), method.Name);
    }

    public MethodMetaData? BuildUpConstructorMethod(Type type)
    {
        var constructorInfo = type.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>());
        if (constructorInfo == default)
        {
            return default;
        }

        var methodName = BuildMethodName(Construct, type);
        var method = BuildMethod(methodName, Array.Empty<Type>(), type);
        var generator = method.GetILGenerator();
        generator.Emit(OpCodes.Newobj, constructorInfo);
        generator.Emit(OpCodes.Ret);
        return new MethodMetaData(typeof(Func<>).MakeGenericType(type), method.Name);
    }

    private static void GenerateEqualCode(ILGenerator generator, Type type, TypeEqualCategory category, LocalBuilder? sourceLocal, LocalBuilder? targetLocal)
    {
        if (sourceLocal != default)
        {
            // nullable value type case
            var getValueOrDefaultMethod = type.GetMethod(GetValueOrDefaultMethodName, Utilities.PublicInstance, Array.Empty<Type>())!;
            var hasValueGetter = type.GetProperty(HasValuePropertyName, Utilities.PublicInstance)!.GetGetMethod()!;
            generator.Emit(OpCodes.Ldloca_S, sourceLocal);
            generator.Emit(OpCodes.Call, getValueOrDefaultMethod);
            generator.Emit(OpCodes.Ldloca_S, targetLocal!);
            generator.Emit(OpCodes.Call, getValueOrDefaultMethod);
            if (category == TypeEqualCategory.NullableOpEquality)
            {
                generator.Emit(OpCodes.Call, type.GenericTypeArguments[0].GetMethod(EqualOperatorMethodName, Utilities.PublicStatic)!);
            }
            else
            {
                generator.Emit(OpCodes.Ceq);
            }

            generator.Emit(OpCodes.Ldloca_S, sourceLocal);
            generator.Emit(OpCodes.Call, hasValueGetter);
            generator.Emit(OpCodes.Ldloca_S, targetLocal!);
            generator.Emit(OpCodes.Call, hasValueGetter);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.And);
        }
        else if (category == TypeEqualCategory.OpEquality)
        {
            generator.Emit(OpCodes.Call, type.GetMethod(EqualOperatorMethodName, Utilities.PublicStatic)!);
        }
        else if (category == TypeEqualCategory.PrimitiveEnumClass)
        {
            generator.Emit(OpCodes.Ceq);
        }
        else
        {
            generator.Emit(OpCodes.Call, ObjectEqual);
        }
    }

    private static TypeEqualCategory GetTypeEqualityCategory(Type type)
    {
        var equalOperator = type.GetMethod(EqualOperatorMethodName, Utilities.PublicStatic);
        if (equalOperator != null && equalOperator.IsHideBySig && equalOperator.IsSpecialName)
        {
            return TypeEqualCategory.OpEquality;
        }

        if (type.IsPrimitive || type.IsEnum || type.IsClass)
        {
            return TypeEqualCategory.PrimitiveEnumClass;
        }

        if (type.IsNullable(out var argumentType))
        {
            equalOperator = argumentType.GetMethod(EqualOperatorMethodName, Utilities.PublicStatic);
            if (equalOperator != null && equalOperator.IsHideBySig && equalOperator.IsSpecialName)
            {
                return TypeEqualCategory.NullableOpEquality;
            }

            if (type.IsPrimitive || type.IsEnum)
            {
                return TypeEqualCategory.NullablePrimitiveEnum;
            }
        }

        return TypeEqualCategory.ObjectEquals;
}

    private static (TypeIsDefaultCategory, bool) GetTypeIsDefaultCategory(Type type)
    {
        if (type.IsClass)
        {
            return (TypeIsDefaultCategory.Class, false);
        }

        if (type.IsNullable(out var argumentType))
        {
            return (GetTypeIsDefaultCategoryForNonNullable(argumentType), true);
        }

        return (GetTypeIsDefaultCategoryForNonNullable(type), false);
    }

    private static TypeIsDefaultCategory GetTypeIsDefaultCategoryForNonNullable(Type type)
    {
        if (type.IsEnum)
        {
            return GetTypeIsDefaultCategoryForNonNullable(Enum.GetUnderlyingType(type));
        }

        if (type == typeof(decimal))
        {
            return TypeIsDefaultCategory.Decimal;
        }

        if (type == typeof(long) || type == typeof(ulong))
        {
            return TypeIsDefaultCategory.Long;
        }

        if (type == typeof(float))
        {
            return TypeIsDefaultCategory.Float;
        }

        if (type == typeof(double))
        {
            return TypeIsDefaultCategory.Double;
        }

        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint))
        {
            return TypeIsDefaultCategory.IntAndBelow;
        }

        return type.GetMethod(EqualOperatorMethodName, Utilities.PublicStatic) != null ? TypeIsDefaultCategory.Struct : throw new InvalidKeyTypeException(type);
    }

    private static void GenerateScalarPropertyConvertingCode(
        ILGenerator generator,
        Type sourcePropertyType,
        Type targetPropertyType,
        Action<ILGenerator> generateLoadDictionaryCode,
        Action<ILGenerator> generateLoadPropertyCode)
    {
        var needToConvert = sourcePropertyType != targetPropertyType;
        var functionType = typeof(Func<,>).MakeGenericType(sourcePropertyType, targetPropertyType);
        if (needToConvert)
        {
            generateLoadDictionaryCode(generator);
            generator.Emit(OpCodes.Ldtoken, sourcePropertyType);
            generator.Emit(OpCodes.Call, TypeOfMethod);
            generator.Emit(OpCodes.Callvirt, ScalarConvertorOuterGetItem);
            generator.Emit(OpCodes.Ldtoken, targetPropertyType);
            generator.Emit(OpCodes.Call, TypeOfMethod);
            generator.Emit(OpCodes.Callvirt, ScalarConvertorInnerGetItem);
            generator.Emit(OpCodes.Castclass, functionType);
        }

        generateLoadPropertyCode(generator);

        if (needToConvert)
        {
            generator.Emit(OpCodes.Callvirt, functionType.GetMethod("Invoke", Utilities.PublicInstance)!);
        }
    }

    private static string GetTypeName(Type type)
    {
        return $"{type.Namespace}_{type.Name}".Replace(".", "_").Replace("`", "_");
    }

    private static string BuildMethodName(char type, Type sourceType, Type targetType)
    {
        return $"_{type}_{GetTypeName(sourceType)}__MapTo__{GetTypeName(targetType)}";
    }

    private static string BuildMethodName(char prefix, Type entityType)
    {
        return $"_{prefix}__{GetTypeName(entityType)}";
    }

    private static string BuildMethodName(Type sourceType, Type targetType)
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
            generator.Emit(OpCodes.Ldarg_3);
            var jumpLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Brtrue_S, jumpLabel);
            GenerateScalarPropertyValueAssignmentIL(generator, sourceConcurrencyTokenProperty!, targetConcurrencyTokenProperty!);
            generator.MarkLabel(jumpLabel);
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
        IRecursiveRegisterContext context)
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
                if (targetProperty.SetMethod != null)
                {
                    recursiveRegister.RegisterEntityDefaultConstructorMethod(targetPropertyType);
                }

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
            generator.Emit(OpCodes.Callvirt, EntityPropertyMapper.MakeGenericMethod(matched.Item2, matched.Item4));
            generator.Emit(OpCodes.Callvirt, matched.Item3.SetMethod!);
        }
    }

    private IList<(PropertyInfo, Type, PropertyInfo, Type)> ExtractEntityListProperties(
        IList<PropertyInfo> sourceProperties,
        IList<PropertyInfo> targetProperties,
        IRecursiveRegister recursiveRegister,
        IRecursiveRegisterContext context)
    {
        var sourceEntityListProperties = sourceProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetEntityListProperties = targetProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false)).ToDictionary(p => p.Name, p => p);
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
                recursiveRegister.RegisterForListItemProperty(sourceItemType, targetItemType);

                if (targetProperty.SetMethod != null)
                {
                    // if the target property can't be set, constructing it when mapping doesn't make sense, need to rely on caller to inialized it before mapping
                    recursiveRegister.RegisterEntityListDefaultConstructorMethod(targetType);
                }

                recursiveRegister.RegisterEntityDefaultConstructorMethod(targetItemType);

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
            if (match.Item3.SetMethod != null)
            {
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Ldarg_3);
                generator.Emit(OpCodes.Callvirt, ListTypeConstructor.MakeGenericMethod(match.Item3.PropertyType, match.Item4));
                generator.Emit(OpCodes.Callvirt, match.Item3.SetMethod!);
            }
            else
            {
                generator.Emit(OpCodes.Ldtoken, match.Item4);
                generator.Emit(OpCodes.Call, TypeOfMethod);
                generator.Emit(OpCodes.Ldstr, match.Item3.Name);
                generator.Emit(OpCodes.Newobj, InitializeOnlyPropertyExceptionContructor);
                generator.Emit(OpCodes.Throw);
            }

            generator.MarkLabel(jumpLabel);
            generator.Emit(OpCodes.Ldarg_3);
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Callvirt, match.Item1.GetMethod!);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Callvirt, match.Item3.GetMethod!);
            generator.Emit(OpCodes.Ldstr, match.Item3.Name);
            generator.Emit(OpCodes.Ldarg, 4);
            generator.Emit(OpCodes.Callvirt, EntityListPropertyMapper.MakeGenericMethod(match.Item2, match.Item4));
        }
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

        GenerateScalarPropertyConvertingCode(
            generator,
            sourcePropertyType,
            targetPropertyType,
            g => g.Emit(OpCodes.Ldarg_2),
            g =>
            {
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Callvirt, sourceProperty.GetMethod!);
            });

        generator.Emit(OpCodes.Callvirt, targetProperty.SetMethod!);
    }
}
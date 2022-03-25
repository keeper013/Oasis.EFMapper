namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;

internal sealed class MapperRegistry
{
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarSourceTypes = new ();
    private readonly HashSet<Type> _convertableToScalarTargetTypes = new ();
    private readonly Dictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeProxyMetaDataSet> _typeProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, ComparerMetaDataSet>> _comparer = new ();

    public MapperRegistry()
    {
        ScalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTargetTypes);
        EntityMapperTypeValidator = new EntityMapperTypeValidator(_mapper, _convertableToScalarSourceTypes, _convertableToScalarTargetTypes);
        EntityListMapperTypeValidator = new EntityListMapperTypeValidator(_mapper, _convertableToScalarSourceTypes, _convertableToScalarTargetTypes);
    }

    public IMapperTypeValidator ScalarMapperTypeValidator { get; }

    public IMapperTypeValidator EntityMapperTypeValidator { get; }

    public IMapperTypeValidator EntityListMapperTypeValidator { get; }

    public void Register(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder)
    {
        if (!EntityMapperTypeValidator.IsSourceType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!EntityMapperTypeValidator.IsTargetType(targetType))
        {
            throw new InvalidEntityTypeException(targetType);
        }

        RecursivelyRegister(sourceType, targetType, new RecursiveRegisterContext(this, methodBuilder));
    }

    public void WithConfiguration(Type type, TypeConfiguration configuration, IDynamicMethodBuilder methodBuilder)
    {
        if (!EntityMapperTypeValidator.IsSourceType(type) && !EntityMapperTypeValidator.IsTargetType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        if (_typesUsingCustomConfiguration.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        if (_typesUsingDefaultConfiguration.Contains(type))
        {
            throw new TypeAlreadyRegisteredException(type);
        }

        _typesUsingCustomConfiguration[type] = configuration;

        var identityPropertyName = configuration.GetIdPropertyName();
        var timeStampPropertyName = configuration.GetTimestampPropertyName();
        GetEntityBaseProperties(type, identityPropertyName, timeStampPropertyName, out var identityProperty, out var timeStampProperty);
        _typeProxies.Add(type, BuildTypeProxy(type, identityProperty, timeStampProperty, methodBuilder, configuration.keepEntityOnMappingRemoved));
    }

    public void WithScalarConverter(Type sourceType, Type targetType, Delegate @delegate)
    {
        var sourceIsScalarType = ScalarMapperTypeValidator.IsSourceType(sourceType);
        var targetIsScalarType = ScalarMapperTypeValidator.IsTargetType(targetType);
        if (!sourceIsScalarType && !targetIsScalarType)
        {
            throw new ScalarTypeMissingException(sourceType, targetType);
        }

        if (!_scalarConverterDictionary.TryGetValue(sourceType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, Delegate>();
            _scalarConverterDictionary[sourceType] = innerDictionary;
        }

        if (!innerDictionary.ContainsKey(targetType))
        {
            innerDictionary.Add(targetType, @delegate);
            if (!sourceIsScalarType)
            {
                _convertableToScalarSourceTypes.Add(sourceType);
            }
            else if (!targetIsScalarType)
            {
                _convertableToScalarTargetTypes.Add(targetType);
            }
        }
        else
        {
            throw new ScalarMapperExistsException(sourceType, targetType);
        }
    }

    public IScalarTypeConverter MakeScalarTypeConverter()
    {
        return new ScalarTypeConverter(_scalarConverterDictionary);
    }

    public MapperSetLookUp MakeMapperSetLookUp(Type type)
    {
        return new MapperSetLookUp(_mapper, type);
    }

    public EntityBaseProxy MakeEntityBaseProxy(Type type, IScalarTypeConverter scalarTypeConverter)
    {
        return new EntityBaseProxy(_typeProxies, _comparer, type, scalarTypeConverter);
    }

    private static void GetEntityBaseProperties(
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

    private void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context)
    {
        if (!context.Contains(sourceType, targetType))
        {
            context.Push(sourceType, targetType);

            RegisterTypeProxies(sourceType, targetType, context.MethodBuilder);

            Dictionary<Type, MapperMetaDataSet>? innerMapper = default;
            Dictionary<Type, ComparerMetaDataSet>? innerComparer = default;
            if (!_comparer.TryGetValue(sourceType, out innerComparer))
            {
                innerComparer = new Dictionary<Type, ComparerMetaDataSet>();
                _comparer[sourceType] = innerComparer;
            }

            if (!_mapper.TryGetValue(sourceType, out innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet>();
                _mapper[sourceType] = innerMapper;
            }

            var methodBuilder = context.MethodBuilder;
            var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
            var targetProperties = targetType.GetProperties(Utilities.PublicInstance);

            if (!innerComparer.ContainsKey(targetType))
            {
                var sourceIdProperty = sourceProperties.First(p => string.Equals(p.Name, GetIdPropertyName(sourceType)));
                var sourceTimeStampProperty = sourceProperties.First(p => string.Equals(p.Name, GetTimeStampPropertyName(sourceType)));
                var targetIdProperty = sourceProperties.First(p => string.Equals(p.Name, GetIdPropertyName(targetType)));
                var targetTimeStampProperty = sourceProperties.First(p => string.Equals(p.Name, GetTimeStampPropertyName(targetType)));
                innerComparer![targetType] = new ComparerMetaDataSet(
                    methodBuilder.BuildUpIdEqualComparerMethod(sourceType, targetType, sourceIdProperty, targetIdProperty),
                    methodBuilder.BuildUpTimeStampEqualComparerMethod(sourceType, targetType, sourceTimeStampProperty, targetTimeStampProperty));
            }

            if (!innerMapper.ContainsKey(targetType))
            {
                innerMapper[targetType] = new MapperMetaDataSet(
                    methodBuilder.BuildUpScalarPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
                    methodBuilder.BuildUpEntityPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context),
                    methodBuilder.BuildUpEntityListPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context));
            }

            context.Pop();
        }
    }

    private void RegisterTypeProxies(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder)
    {
        var sourceTypeRegistered = _typesUsingCustomConfiguration.ContainsKey(sourceType) || _typesUsingDefaultConfiguration.Contains(sourceType);
        var targetTypeRegistered = _typesUsingCustomConfiguration.ContainsKey(targetType) || _typesUsingDefaultConfiguration.Contains(targetType);
        if (!sourceTypeRegistered || !targetTypeRegistered)
        {
            GetEntityBaseProperties(sourceType, GetIdPropertyName(sourceType), GetTimeStampPropertyName(sourceType), out var sourceIdProperty, out var sourceTimeStampProperty);
            GetEntityBaseProperties(targetType, GetIdPropertyName(targetType), GetTimeStampPropertyName(targetType), out var targetIdProperty, out var targetTimeStampProperty);

            var sourceIdType = sourceIdProperty.PropertyType;
            var targetIdType = targetIdProperty.PropertyType;
            if (sourceIdType != targetIdType && !ScalarMapperTypeValidator.CanConvert(sourceIdType, targetIdType))
            {
                throw new ScalarConverterMissingException(sourceIdType, targetIdType);
            }

            var sourceTimeStampType = sourceTimeStampProperty.PropertyType;
            var targetTimeStampType = targetTimeStampProperty.PropertyType;
            if (sourceTimeStampType != targetTimeStampType && !ScalarMapperTypeValidator.CanConvert(sourceTimeStampType, targetTimeStampType))
            {
                throw new ScalarConverterMissingException(sourceTimeStampType, targetTimeStampType);
            }

            if (!sourceTypeRegistered)
            {
                _typesUsingDefaultConfiguration.Add(sourceType);
                _typeProxies.Add(sourceType, BuildTypeProxy(sourceType, sourceIdProperty, sourceTimeStampProperty, methodBuilder));
            }

            if (!targetTypeRegistered && sourceType != targetType)
            {
                _typesUsingDefaultConfiguration.Add(targetType);
                _typeProxies.Add(targetType, BuildTypeProxy(targetType, targetIdProperty, targetTimeStampProperty, methodBuilder));
            }
        }
    }

    private TypeProxyMetaDataSet BuildTypeProxy(
        Type type,
        PropertyInfo identityProperty,
        PropertyInfo timeStampProperty,
        IDynamicMethodBuilder methodBuilder,
        bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved)
    {
        return new TypeProxyMetaDataSet(
            methodBuilder.BuildUpGetIdMethod(type, identityProperty),
            methodBuilder.BuildUpIdIsEmptyMethod(type, identityProperty),
            methodBuilder.BuildUpTimeStampIsEmptyMethod(type, timeStampProperty),
            identityProperty,
            keepEntityOnMappingRemoved);
    }

    private string GetIdPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetIdPropertyName() : MapperRegistryExtensions.DefaultIdPropertyName;
    }

    private string GetTimeStampPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetTimestampPropertyName() : MapperRegistryExtensions.DefaultTimestampPropertyName;
    }
}

internal static class MapperRegistryExtensions
{
    internal const string DefaultIdPropertyName = "Id";
    internal const string DefaultTimestampPropertyName = "Timestamp";

    public static string GetIdPropertyName(this TypeConfiguration configuration)
    {
        return configuration.identityPropertyName ?? DefaultIdPropertyName;
    }

    public static string GetTimestampPropertyName(this TypeConfiguration configuration)
    {
        return configuration.timestampPropertyName ?? DefaultTimestampPropertyName;
    }
}

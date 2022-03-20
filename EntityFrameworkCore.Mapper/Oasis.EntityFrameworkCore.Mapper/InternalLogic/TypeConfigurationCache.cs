namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Reflection;
using System.Reflection.Emit;

internal sealed class TypeConfigurationCache
{
    private static readonly IReadOnlySet<Type> IdTypes = new HashSet<Type>
    {
        typeof(int), typeof(long), typeof(uint), typeof(ulong), typeof(string), typeof(short), typeof(ushort), typeof(byte),
    };

    private static readonly IReadOnlySet<Type> TimestampTypes = new HashSet<Type>
    {
        typeof(byte[]), typeof(DateTime), typeof(string), typeof(int), typeof(long), typeof(uint), typeof(ulong),
        typeof(short), typeof(ushort), typeof(byte),
    };

    private readonly IDynamicMethodBuilder _methodBuilder;
    private readonly ScalarConverterCache _scalarConverterCache;
    private readonly Dictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeProxyMetaDataSet> _typeProxies = new ();

    public TypeConfigurationCache(IDynamicMethodBuilder methodBuilder, ScalarConverterCache scalarConverterCache)
    {
        _methodBuilder = methodBuilder;
        _scalarConverterCache = scalarConverterCache;
    }

    public IReadOnlyDictionary<Type, TypeProxyMetaDataSet> Export() => _typeProxies;

    public void AddConfiguration(Type type, TypeConfiguration configuration)
    {
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
        _typeProxies.Add(type, BuildTypeProxy(type, identityProperty, timeStampProperty, configuration.keepEntityOnMappingRemoved));
    }

    public void ValidateEntityBaseProperties(Type sourceType, Type targetType)
    {
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
                _typeProxies.Add(sourceType, BuildTypeProxy(sourceType, sourceIdProperty, sourceTimeStampProperty));
            }

            if (!targetTypeValidated)
            {
                _typesUsingDefaultConfiguration.Add(targetType);
                _typeProxies.Add(targetType, BuildTypeProxy(targetType, targetIdProperty, targetTimeStampProperty));
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

    private string GetIdPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetIdPropertyName() : Utilities.DefaultIdPropertyName;
    }

    private string GetTimeStampPropertyName(Type type)
    {
        return _typesUsingCustomConfiguration.TryGetValue(type, out var configuration) ? configuration.GetTimestampPropertyName() : Utilities.DefaultTimestampPropertyName;
    }

    private TypeProxyMetaDataSet BuildTypeProxy(
        Type type,
        PropertyInfo identityProperty,
        PropertyInfo timeStampProperty,
        bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved)
    {
        return new TypeProxyMetaDataSet(
            BuildUpGetIdMethod(type, identityProperty),
            BuildUpIdIsEmptyMethod(type, identityProperty),
            BuildUpTimeStampIsEmptyMethod(type, timeStampProperty),
            identityProperty,
            keepEntityOnMappingRemoved);
    }

    private MethodMetaData BuildUpGetIdMethod(Type type, PropertyInfo identityProperty)
    {
        throw new NotImplementedException();
    }

    private MethodMetaData BuildUpIdIsEmptyMethod(Type type, PropertyInfo identityProperty)
    {
        throw new NotImplementedException();
    }

    private MethodMetaData BuildUpTimeStampIsEmptyMethod(Type type, PropertyInfo timeStampProperty)
    {
        throw new NotImplementedException();
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

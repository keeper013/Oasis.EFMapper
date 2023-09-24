namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Collections.Generic;
using System.Reflection.Emit;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal record struct KeyPropertyConfiguration(string identityPropertyName, string? concurrencyTokenPropertyName = default);

internal sealed class MapperRegistry : IRecursiveRegister
{
    private readonly DynamicMethodBuilder _dynamicMethodBuilder;
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarTypes = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeIdProxies = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeConcurrencyTokenProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet?>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _idComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _concurrencyTokenComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapToDatabaseType>> _mapToDatabaseDictionary = new ();
    private readonly Dictionary<Type, Dictionary<Type, ICustomTypeMapperConfiguration?>> _toBeRegistered = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _sourceIdEqualsTargetId = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _sourceIdListContainsTargetId = new ();
    private readonly Dictionary<Type, Dictionary<Type, Dictionary<Type, TargetByIdTrackerMetaDataSet>>> _targetByIdTrackers = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping = new ();
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _entityListFactoryMethods = new ();
    private readonly Dictionary<Type, MethodMetaData> _entityDefaultConstructors = new ();
    private readonly Dictionary<Type, MethodMetaData> _entityListDefaultConstructors = new ();
    private readonly IMapperTypeValidator _scalarMapperTypeValidator;
    private readonly IMapperTypeValidator _entityMapperTypeValidator;
    private readonly KeyPropertyNameManager _keyPropertyNames;
    private readonly ExcludedPropertyManager _excludedPropertyManager;
    private readonly KeepUnmatchedManager _keepUnmatchedManager;
    private readonly MapToDatabaseType _defaultMapToDatabase;

    public MapperRegistry(ModuleBuilder module, IMapperBuilderConfiguration? configuration)
    {
        _defaultMapToDatabase = configuration?.MapToDatabaseType ?? MapToDatabaseType.Upsert;
        _keyPropertyNames = new (new KeyPropertyNameConfiguration(configuration?.IdentityPropertyName, configuration?.ConcurrencyTokenPropertyName));
        _scalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTypes);
        _entityMapperTypeValidator = new EntityMapperTypeValidator(_mapper);
        _excludedPropertyManager = new (configuration?.ExcludedProperties);
        _keepUnmatchedManager = new ();
        _dynamicMethodBuilder = new (
            module.DefineType("Mapper", TypeAttributes.Public),
            _scalarMapperTypeValidator,
            _entityMapperTypeValidator,
            new EntityListMapperTypeValidator(_mapper));
    }

    public KeepUnmatchedManager KeepUnmatchedManager => _keepUnmatchedManager;

    public void Register(Type sourceType, Type targetType)
    {
        _toBeRegistered.AddIfNotExists(sourceType, targetType, (ICustomTypeMapperConfiguration?)null);
    }

    public Type Build()
    {
        while (_toBeRegistered.Any())
        {
            var firstOuter = _toBeRegistered.First();
            var firstInner = firstOuter.Value.First();
            RegisterAndPop(firstOuter.Key, firstInner.Key);
        }

        return _dynamicMethodBuilder.Build();
    }

    public void Configure(Type sourceType, Type targetType, ICustomTypeMapperConfiguration configuration)
    {
        if (configuration.ExcludedProperties != null)
        {
            _excludedPropertyManager.Add(sourceType, targetType, configuration.ExcludedProperties);
        }

        if (configuration.KeepUnmatchedProperties != null)
        {
            _keepUnmatchedManager.Add(sourceType, targetType, configuration.KeepUnmatchedProperties);
        }

        _toBeRegistered.AddOrUpdateNull(sourceType, targetType, configuration);
    }

    public void Configure(Type type, IEntityConfiguration configuration)
    {
        if (!_entityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        if (!string.IsNullOrEmpty(configuration.IdentityPropertyName))
        {
            _keyPropertyNames.Add(type, new KeyPropertyConfiguration(configuration.IdentityPropertyName, configuration.ConcurrencyTokenPropertyName));
        }

        if (configuration.ExcludedProperties != null && configuration.ExcludedProperties.Any())
        {
            _excludedPropertyManager.Add(type, configuration.ExcludedProperties);
        }

        if (configuration.KeepUnmatchedProperties != null && configuration.KeepUnmatchedProperties.Any())
        {
            _keepUnmatchedManager.Add(type, configuration.KeepUnmatchedProperties);
        }
    }

    public void WithFactoryMethod(Type type, Delegate factoryMethod, bool throwIfRedundant = false)
    {
        if (type.IsListOfEntityType())
        {
            if (_entityListFactoryMethods.ContainsKey(type))
            {
                if (throwIfRedundant)
                {
                    throw new FactoryMethodExistsException(type);
                }
            }
            else
            {
                _entityListFactoryMethods.Add(type, factoryMethod);
            }
        }
        else if (type.IsEntityType())
        {
            if (_factoryMethods.ContainsKey(type))
            {
                if (throwIfRedundant)
                {
                    throw new FactoryMethodExistsException(type);
                }
            }
            else
            {
                _factoryMethods.Add(type, factoryMethod!);
            }
        }
        else
        {
            throw new InvalidFactoryMethodEntityTypeException(type);
        }
    }

    public void WithScalarConverter(Type sourceType, Type targetType, Delegate @delegate, bool throwIfRedundant = false)
    {
        var sourceIsScalarType = _scalarMapperTypeValidator.IsValidType(sourceType);
        var targetIsScalarType = _scalarMapperTypeValidator.IsValidType(targetType);
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
                _convertableToScalarTypes.Add(sourceType);
            }
            else if (!targetIsScalarType)
            {
                _convertableToScalarTypes.Add(targetType);
            }
        }
        else if (throwIfRedundant)
        {
            throw new ScalarMapperExistsException(sourceType, targetType);
        }
    }

    public bool IsConfigured(Type sourceType, Type targetType) => _toBeRegistered.Find(sourceType, targetType) != null;

    public bool IsConfigured(Type entityType) => _keyPropertyNames.ContainsConfiguration(entityType)
        || _excludedPropertyManager.ContainsTypeConfiguration(entityType) || _keepUnmatchedManager.ContainsTypeConfiguration(entityType);

    public IScalarTypeConverter MakeScalarTypeConverter()
    {
        return new ScalarTypeConverter(_scalarConverterDictionary);
    }

    public MapperSetLookUp MakeMapperSetLookUp(Type type)
    {
        return new MapperSetLookUp(_mapper, type);
    }

    public EntityHandlerData MakeEntityHandler(Type type)
    {
        return new EntityHandlerData(
            MakeTypeKeyProxyDictionary(_typeIdProxies, type),
            MakeTypeKeyProxyDictionary(_typeConcurrencyTokenProxies, type),
            MakeDelegateDictionary(_idComparers, type),
            MakeDelegateDictionary(_concurrencyTokenComparers, type),
            MakeDelegateDictionary(_sourceIdEqualsTargetId, type),
            MakeDelegateDictionary(_sourceIdListContainsTargetId, type),
            MakeFactoryMethods(_factoryMethods, _entityDefaultConstructors, type),
            MakeFactoryMethods(_entityListFactoryMethods, _entityListDefaultConstructors, type));
    }

    public MapToDatabaseTypeManager MakeMapToDatabaseTypeManager()
    {
        return new MapToDatabaseTypeManager(_defaultMapToDatabase, _mapToDatabaseDictionary);
    }

    public EntityTrackerData MakeEntityTrackerData(Type type, IScalarTypeConverter scalarTypeConverter)
    {
        var targetIdentityTypeMapping = new Dictionary<Type, Type>();
        var targetByIdTrackerFactories = new Dictionary<Type, ITargetByIdTrackerFactory>();
        foreach (var kvp in _targetByIdTrackers)
        {
            var factory = (ITargetByIdTrackerFactory)Activator.CreateInstance(typeof(TargetByIdTrackerFactory<>).MakeGenericType(kvp.Key), kvp.Value, scalarTypeConverter, type)!;
            targetByIdTrackerFactories.Add(kvp.Key, factory);
            foreach (var targetType in kvp.Value.SelectMany(v => v.Value.Keys).Distinct())
            {
                targetIdentityTypeMapping.Add(targetType, kvp.Key);
            }
        }

        Dictionary<Type, IReadOnlySet<Type>>? loopDependencies = null;
        if (_loopDependencyMapping.Any())
        {
            loopDependencies = new Dictionary<Type, IReadOnlySet<Type>>();
            foreach (var kvp in _loopDependencyMapping)
            {
                loopDependencies.Add(kvp.Key, new HashSet<Type>(kvp.Value));
            }
        }

        return new EntityTrackerData(targetIdentityTypeMapping, targetByIdTrackerFactories, loopDependencies);
    }

    public void RegisterEntityListDefaultConstructorMethod(Type listType)
    {
        if (!_entityListFactoryMethods.ContainsKey(listType) && listType.IsConstructable() && !listType.IsList() && !_entityListDefaultConstructors.ContainsKey(listType))
        {
            var constructor = _dynamicMethodBuilder.BuildUpConstructorMethod(listType);
            if (constructor.HasValue)
            {
                _entityListDefaultConstructors.Add(listType, constructor.Value);
            }
        }
    }

    public void RegisterEntityDefaultConstructorMethod(Type type)
    {
        if (!_entityDefaultConstructors.ContainsKey(type) && !_factoryMethods.ContainsKey(type))
        {
            var constructor = _dynamicMethodBuilder.BuildUpConstructorMethod(type);
            if (constructor.HasValue)
            {
                _entityDefaultConstructors.Add(type, constructor.Value);
            }
        }
    }

    public void RegisterForListItemProperty(Type sourceListItemPropertyType, Type targetListItemPropertyType)
    {
        if (!_sourceIdListContainsTargetId.Contains(sourceListItemPropertyType, targetListItemPropertyType))
        {
            var sourceIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(sourceListItemPropertyType);
            var sourceIdentityProperty = string.IsNullOrEmpty(sourceIdentityPropertyName) ? default : sourceListItemPropertyType.GetProperty(sourceIdentityPropertyName, Utilities.PublicInstance);
            if (sourceIdentityProperty != default && !sourceIdentityProperty.VerifyGetterSetter(false))
            {
                sourceIdentityProperty = default;
            }

            var targetIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(targetListItemPropertyType);
            var targetIdentityProperty = string.IsNullOrEmpty(targetIdentityPropertyName) ? default : targetListItemPropertyType.GetProperty(targetIdentityPropertyName, Utilities.PublicInstance);
            if (targetIdentityProperty != default && !targetIdentityProperty.VerifyGetterSetter(true))
            {
                targetIdentityProperty = default;
            }

            if (sourceIdentityProperty != default && targetIdentityProperty != default)
            {
                _sourceIdListContainsTargetId.AddIfNotExists(
                    sourceListItemPropertyType,
                    targetListItemPropertyType,
                    () => _dynamicMethodBuilder.BuildUpSourceIdListContainsTargetIdMethod(sourceListItemPropertyType, sourceIdentityProperty, targetListItemPropertyType, targetIdentityProperty));
            }
        }
    }

    public void RecursivelyRegister(Type sourceType, Type targetType, IRecursiveRegisterContext context)
    {
        if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>()) == default)
        {
            throw new FactoryMethodException(targetType, true);
        }

        var configuration = _toBeRegistered.Pop(sourceType, targetType);

        try
        {
            context.Push(sourceType, targetType);
            var (sourceExcludedProperties, targetExcludedProperties) = _excludedPropertyManager.GetExcludedPropertyNames(sourceType, targetType);
            var sourceProperties = sourceExcludedProperties != null
                ? sourceType.GetProperties(Utilities.PublicInstance).Where(p => !sourceExcludedProperties.Contains(p.Name)).ToList()
                : sourceType.GetProperties(Utilities.PublicInstance).ToList();
            var targetProperties = targetExcludedProperties != null
                ? targetType.GetProperties(Utilities.PublicInstance).Where(p => !targetExcludedProperties.Contains(p.Name)).ToList()
                : targetType.GetProperties(Utilities.PublicInstance).ToList();
            if (configuration?.CustomPropertyMapper != null)
            {
                targetProperties = targetProperties.Except(configuration.CustomPropertyMapper.MappedTargetProperties).ToList();
            }

            var (sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty) =
                ExtractKeyProperties(sourceType, targetType, sourceProperties, targetProperties, sourceExcludedProperties, targetExcludedProperties);

            RegisterSourceIdEqualsTargetIdMethod(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty);
            RegisterKeyProperty(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder, _typeIdProxies, KeyType.Id);
            RegisterKeyProperty(sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder, _typeConcurrencyTokenProxies, KeyType.ConcurrencyToken);
            RegisterKeyComparer(KeyType.Id, _idComparers, sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder);
            RegisterKeyComparer(KeyType.ConcurrencyToken, _concurrencyTokenComparers, sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder);
            RegisterTargetByIdTracker(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty);

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet?>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet?>();
                _mapper[sourceType] = innerMapper;
            }

            if (!innerMapper.ContainsKey(targetType))
            {
                if (configuration?.MapToDatabaseType != null)
                {
                    _mapToDatabaseDictionary.AddIfNotExists(sourceType, targetType, configuration.MapToDatabaseType.Value);
                }

                var keyMapper = _dynamicMethodBuilder.BuildUpKeyPropertiesMapperMethod(
                    sourceType,
                    targetType,
                    sourceIdentityProperty,
                    targetIdentityProperty,
                    sourceConcurrencyTokenProperty,
                    targetConcurrencyTokenProperty);

                var contentMapper = _dynamicMethodBuilder.BuildUpContentMappingMethod(
                    sourceType,
                    targetType,
                    sourceProperties,
                    targetProperties,
                    this,
                    context);
                innerMapper[targetType] = Utilities.BuildMapperMetaDataSet(configuration?.CustomPropertyMapper?.MapProperties, keyMapper, contentMapper);
            }
        }
        finally
        {
            context.Pop();
        }
    }

    private static TypeKeyProxyMetaDataSet BuildTypeKeyProxy(
        Type type,
        PropertyInfo property,
        DynamicMethodBuilder methodBuilder,
        KeyType keyType)
    {
        return new TypeKeyProxyMetaDataSet(
            methodBuilder.BuildUpKeyIsEmptyMethod(keyType, type, property),
            property);
    }

    private static void RegisterKeyComparer(
        KeyType keyType,
        Dictionary<Type, Dictionary<Type, MethodMetaData>> comparers,
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceKeyProperty,
        PropertyInfo? targetKeyProperty,
        DynamicMethodBuilder methodBuilder)
    {
        comparers.AddIfNotExists(
            sourceType,
            targetType,
            () => methodBuilder.BuildUpKeyEqualComparerMethod(keyType, sourceType, targetType, sourceKeyProperty!, targetKeyProperty!),
            sourceKeyProperty != default && targetKeyProperty != default);
    }

    private static Dictionary<Type, TypeKeyProxy> MakeTypeKeyProxyDictionary(Dictionary<Type, TypeKeyProxyMetaDataSet> proxies, Type type)
    {
        var result = new Dictionary<Type, TypeKeyProxy>();
        foreach (var pair in proxies)
        {
            var typeKeyDataSet = pair.Value;
            var proxy = new TypeKeyProxy(
                Delegate.CreateDelegate(typeKeyDataSet.isEmpty.type, type.GetMethod(typeKeyDataSet.isEmpty.name)!),
                typeKeyDataSet.property);
            result.Add(pair.Key, proxy);
        }

        return result;
    }

    private static Dictionary<Type, Delegate> MakeFactoryMethods(IReadOnlyDictionary<Type, Delegate> direct, IReadOnlyDictionary<Type, MethodMetaData> generated, Type type)
    {
        var factoryMethods = new Dictionary<Type, Delegate>(direct);
        foreach (var kvp in generated)
        {
            factoryMethods.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name)!));
        }

        return factoryMethods;
    }

    private static Dictionary<TKey1, IReadOnlyDictionary<TKey2, Delegate>> MakeDelegateDictionary<TKey1, TKey2>(IReadOnlyDictionary<TKey1, Dictionary<TKey2, MethodMetaData>> metaDataDict, Type type)
        where TKey1 : notnull
        where TKey2 : notnull
    {
        var result = new Dictionary<TKey1, IReadOnlyDictionary<TKey2, Delegate>>();
        foreach (var pair in metaDataDict)
        {
            var innerDictionary = new Dictionary<TKey2, Delegate>();
            foreach (var innerPair in pair.Value)
            {
                var comparer = innerPair.Value;
                var @delegate = Delegate.CreateDelegate(comparer.type, type.GetMethod(comparer.name)!);
                innerDictionary.Add(innerPair.Key, @delegate);
            }

            result.Add(pair.Key, innerDictionary);
        }

        return result;
    }

    private void RegisterAndPop(Type sourceType, Type targetType)
    {
        if (!_entityMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!_entityMapperTypeValidator.IsValidType(targetType))
        {
            throw new InvalidEntityTypeException(targetType);
        }

        RegisterEntityDefaultConstructorMethod(targetType);
        RecursivelyRegister(sourceType, targetType, new RecursiveRegisterContext(_loopDependencyMapping));
    }

    private (PropertyInfo?, PropertyInfo?, PropertyInfo?, PropertyInfo?) ExtractKeyProperties(
        Type sourceType,
        Type targetType,
        IList<PropertyInfo> sourceProperties,
        IList<PropertyInfo> targetProperties,
        ISet<string>? sourceExcludedProperties,
        ISet<string>? targetExcludedProperties)
    {
        var sourceIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(sourceType);
        var targetIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(targetType);
        var sourceConcurrencyTokenPropertyName = _keyPropertyNames.GetConcurrencyTokenPropertyName(sourceType);
        var targetConcurrencyTokenPropertyName = _keyPropertyNames.GetConcurrencyTokenPropertyName(targetType);
        if (sourceExcludedProperties != null)
        {
            if (sourceIdentityPropertyName != null && sourceExcludedProperties.Contains(sourceIdentityPropertyName))
            {
                throw new KeyTypeExcludedException(sourceType, targetType, sourceIdentityPropertyName);
            }

            if (sourceConcurrencyTokenPropertyName != null && sourceExcludedProperties.Contains(sourceConcurrencyTokenPropertyName))
            {
                throw new KeyTypeExcludedException(sourceType, targetType, sourceConcurrencyTokenPropertyName);
            }
        }

        if (targetExcludedProperties != null)
        {
            if (targetIdentityPropertyName != null && targetExcludedProperties.Contains(targetIdentityPropertyName))
            {
                throw new KeyTypeExcludedException(sourceType, targetType, targetIdentityPropertyName);
            }

            if (targetConcurrencyTokenPropertyName != null && targetExcludedProperties.Contains(targetConcurrencyTokenPropertyName))
            {
                throw new KeyTypeExcludedException(sourceType, targetType, targetConcurrencyTokenPropertyName);
            }
        }

        var sourceIdentityProperty = sourceProperties.GetKeyProperty(sourceIdentityPropertyName, false);
        var targetIdentityProperty = targetProperties.GetKeyProperty(targetIdentityPropertyName, true);
        var sourceConcurrencyTokenProperty = sourceProperties.GetKeyProperty(sourceConcurrencyTokenPropertyName, false);
        var targetConcurrencyTokenProperty = targetProperties.GetKeyProperty(targetConcurrencyTokenPropertyName, true);
        if (sourceIdentityProperty != null)
        {
            sourceProperties.Remove(sourceIdentityProperty);
        }

        if (targetIdentityProperty != null)
        {
            targetProperties.Remove(targetIdentityProperty);
        }

        if (sourceConcurrencyTokenProperty != null)
        {
            sourceProperties.Remove(sourceConcurrencyTokenProperty);
        }

        if (targetConcurrencyTokenProperty != null)
        {
            targetProperties.Remove(targetConcurrencyTokenProperty);
        }

        return (sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
    }

    private void RegisterSourceIdEqualsTargetIdMethod(Type sourceType, PropertyInfo? sourceIdentityProperty, Type targetType, PropertyInfo? targetIdentityProperty)
    {
        if (sourceIdentityProperty != null && targetIdentityProperty != null)
        {
            _sourceIdEqualsTargetId.AddIfNotExists(
                sourceType,
                targetType,
                () => _dynamicMethodBuilder.BuildUpSourceIdEqualsTargetIdMethod(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty));
        }
    }

    private void RegisterKeyProperty(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceProperty,
        PropertyInfo? targetProperty,
        DynamicMethodBuilder methodBuilder,
        Dictionary<Type, TypeKeyProxyMetaDataSet> typeKeyProxies,
        KeyType keyType)
    {
        var sourcePropertyExists = sourceProperty != default;
        var targetPropertyExists = targetProperty != default;
        if (sourcePropertyExists && targetPropertyExists)
        {
            var sourceIdType = sourceProperty!.PropertyType;
            var targetIdType = targetProperty!.PropertyType;
            if (sourceIdType != targetIdType && !_scalarMapperTypeValidator.CanConvert(sourceIdType, targetIdType))
            {
                throw new ScalarConverterMissingException(sourceIdType, targetIdType);
            }
        }

        if (sourcePropertyExists && !typeKeyProxies.ContainsKey(sourceType))
        {
            typeKeyProxies.Add(sourceType, BuildTypeKeyProxy(sourceType, sourceProperty!, methodBuilder, keyType));
        }

        if (targetPropertyExists && !typeKeyProxies.ContainsKey(targetType))
        {
            typeKeyProxies.Add(targetType, BuildTypeKeyProxy(targetType, targetProperty!, methodBuilder, keyType));
        }
    }

    private void RegisterTargetByIdTracker(Type sourceType, Type targetType, PropertyInfo? sourceIdentityProperty, PropertyInfo? targetIdentityProperty)
    {
        if (sourceIdentityProperty != default && targetIdentityProperty != default)
        {
            var targetKeyUnderlyingType = targetIdentityProperty.PropertyType.GetUnderlyingType();
            if (!_targetByIdTrackers.TryGetValue(targetKeyUnderlyingType, out var trackers))
            {
                trackers = new Dictionary<Type, Dictionary<Type, TargetByIdTrackerMetaDataSet>>();
                _targetByIdTrackers.Add(targetKeyUnderlyingType, trackers);
            }

            trackers.AddIfNotExists(sourceType, targetType, () => new TargetByIdTrackerMetaDataSet(
                _dynamicMethodBuilder.BuildUpTargetByIdTrackerFindMethod(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty),
                _dynamicMethodBuilder.BuildUpTargetByIdTrackerTrackMethod(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty)));
        }
    }
}

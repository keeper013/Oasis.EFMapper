namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal record struct KeyPropertyConfiguration(string identityPropertyName,
    string? concurrencyTokenPropertyName = default);

internal sealed class MapperRegistry
{
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarTypes = new ();
    private readonly HashSet<Type> _knownEntityTypes = new ();
    private readonly Dictionary<Type, KeyPropertyConfiguration> _keyPropertyConfiguration = new ();
    private readonly Dictionary<Type, bool> _typeKeepEntityOnMappingRemovedConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeIdProxies = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeConcurrencyTokenProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet?>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _idComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _concurrencyTokenComparers = new ();
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _typeListFactoryMethods = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping = new ();
    private readonly IMapperTypeValidator _scalarMapperTypeValidator;
    private readonly IMapperTypeValidator _entityMapperTypeValidator;
    private readonly IMapperTypeValidator _entityListMapperTypeValidator;
    private readonly IKeyPropertyNameManager _keyPropertyNames;

    public MapperRegistry(EntityConfiguration defaultConfiguration)
    {
        _keyPropertyNames = new KeyPropertyNameManager(
            new KeyPropertyNameConfiguration(defaultConfiguration.identityPropertyName, defaultConfiguration.concurrencyTokenPropertyName),
            _keyPropertyConfiguration);
        _scalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTypes);
        _entityMapperTypeValidator = new EntityMapperTypeValidator(_mapper, _convertableToScalarTypes);
        _entityListMapperTypeValidator = new EntityListMapperTypeValidator(_mapper, _convertableToScalarTypes);
    }

    public void Register(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder, ICustomPropertyMapperInternal? customPropertyMapper)
    {
        if (!_entityMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!_entityMapperTypeValidator.IsValidType(targetType))
        {
            throw new InvalidEntityTypeException(targetType);
        }

        RecursivelyRegister(sourceType, targetType, new RecursiveRegisterContext(this, methodBuilder, _loopDependencyMapping), customPropertyMapper, true);
    }

    public void RegisterTwoWay(
        Type sourceType,
        Type targetType,
        IDynamicMethodBuilder methodBuilder,
        ICustomPropertyMapperInternal? customPropertyMapperSourceToTarget,
        ICustomPropertyMapperInternal? customPropertyMapperTargetToSource)
    {
        if (!_entityMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!_entityMapperTypeValidator.IsValidType(targetType))
        {
            throw new InvalidEntityTypeException(targetType);
        }

        var context = new RecursiveRegisterContext(this, methodBuilder, _loopDependencyMapping);
        RecursivelyRegister(sourceType, targetType, context, customPropertyMapperSourceToTarget, true);
        RecursivelyRegister(targetType, sourceType, context, customPropertyMapperTargetToSource, true);
    }

    public void WithFactoryMethod(Type type, Type itemType, Delegate factoryMethod, bool throwIfRedundant = false)
    {
        if (factoryMethod == default)
        {
            throw new ArgumentNullException(nameof(factoryMethod));
        }

        if (!_entityMapperTypeValidator.IsValidType(itemType))
        {
            throw new InvalidEntityListTypeException(type);
        }

        if (_typeListFactoryMethods.ContainsKey(type))
        {
            if (throwIfRedundant)
            {
                throw new FactoryMethodExistsException(type);
            }
        }
        else
        {
            _typeListFactoryMethods.Add(type, factoryMethod);
            _knownEntityTypes.Add(itemType);
        }
    }

    public void WithFactoryMethod(Type type, Delegate factoryMethod, bool throwIfRedundant = false)
    {
        if (factoryMethod == default)
        {
            throw new ArgumentNullException(nameof(factoryMethod));
        }

        if (type.GetConstructor(Array.Empty<Type>()) != default)
        {
            throw new FactoryMethodException(type, false);
        }

        if (!_entityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

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

    public void WithConfiguration(Type type, EntityConfiguration configuration, IDynamicMethodBuilder methodBuilder, bool throwIfRedundant = false)
    {
        if (!_entityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        bool typeIsConfigurated = false;
        if (_keyPropertyConfiguration.ContainsKey(type) || _typeKeepEntityOnMappingRemovedConfiguration.ContainsKey(type))
        {
            if (throwIfRedundant)
            {
                throw new TypeConfiguratedException(type);
            }

            typeIsConfigurated = true;
        }

        if (_typesUsingDefaultConfiguration.Contains(type))
        {
            typeIsConfigurated = true;
            if (throwIfRedundant)
            {
                throw new TypeAlreadyRegisteredException(type);
            }
        }

        if (!typeIsConfigurated)
        {
            _keyPropertyConfiguration[type] = new KeyPropertyConfiguration(configuration.identityPropertyName, configuration.concurrencyTokenPropertyName);

            var identityProperty = type.GetProperties(Utilities.PublicInstance).GetKeyProperty(configuration.identityPropertyName, false);
            if (identityProperty != default)
            {
                _typeIdProxies.Add(type, BuildTypeKeyProxy(type, identityProperty, methodBuilder, KeyType.Id));
            }

            var concurrencyTokenProperty = type.GetProperties(Utilities.PublicInstance).GetKeyProperty(configuration.concurrencyTokenPropertyName, false);
            if (concurrencyTokenProperty != default)
            {
                _typeConcurrencyTokenProxies.Add(type, BuildTypeKeyProxy(type, concurrencyTokenProperty, methodBuilder, KeyType.ConcurrencyToken));
            }

            if (configuration.keepEntityOnMappingRemoved.HasValue)
            {
                if (identityProperty == null)
                {
                    throw new MissingIdentityException(type);
                }

                _typeKeepEntityOnMappingRemovedConfiguration.Add(type, configuration.keepEntityOnMappingRemoved.Value);
            }
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

        if (IsKnownEntityType(sourceType) || _entityListMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidScalarTypeException(sourceType);
        }

        if (IsKnownEntityType(targetType) || _entityListMapperTypeValidator.IsValidType(targetType))
        {
            throw new InvalidScalarTypeException(targetType);
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

    public IScalarTypeConverter MakeScalarTypeConverter()
    {
        return new ScalarTypeConverter(_scalarConverterDictionary);
    }

    public IListTypeConstructor MakeListTypeConstructor()
    {
        return new ListTypeConstructor(_typeListFactoryMethods);
    }

    public MapperSetLookUp MakeMapperSetLookUp(Type type)
    {
        return new MapperSetLookUp(_mapper, type);
    }

    public EntityBaseProxy MakeEntityBaseProxy(Type type, IScalarTypeConverter scalarTypeConverter)
    {
        return new EntityBaseProxy(_typeIdProxies, _typeConcurrencyTokenProxies, _idComparers, _concurrencyTokenComparers, type, scalarTypeConverter);
    }

    public EntityFactory MakeEntityFactory()
    {
        return new EntityFactory(_factoryMethods);
    }

    public TargetTrackerProvider MakeTargetTrackerProvider(IEntityFactory entityFactory)
    {
        return new TargetTrackerProvider(_loopDependencyMapping, entityFactory);
    }

    public void Clear()
    {
        _scalarConverterDictionary.Clear();
        _convertableToScalarTypes.Clear();
        _knownEntityTypes.Clear();
        _keyPropertyConfiguration.Clear();
        _typesUsingDefaultConfiguration.Clear();
        _typeIdProxies.Clear();
        _typeConcurrencyTokenProxies.Clear();
        _mapper.Clear();
        _idComparers.Clear();
        _concurrencyTokenComparers.Clear();
    }

    private static TypeKeyProxyMetaDataSet BuildTypeKeyProxy(
        Type type,
        PropertyInfo property,
        IDynamicMethodBuilder methodBuilder,
        KeyType keyType)
    {
        return new TypeKeyProxyMetaDataSet(
            keyType == KeyType.Id ? methodBuilder.BuildUpGetIdMethod(keyType, type, property) : null,
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
        IDynamicMethodBuilder methodBuilder)
    {
        if (!comparers.TryGetValue(sourceType, out Dictionary<Type, MethodMetaData>? innerComparer))
        {
            innerComparer = new Dictionary<Type, MethodMetaData>();
            comparers[sourceType] = innerComparer;
        }

        if (!innerComparer.ContainsKey(targetType) && sourceKeyProperty != default && targetKeyProperty != default)
        {
            innerComparer![targetType] = methodBuilder.BuildUpKeyEqualComparerMethod(keyType, sourceType, targetType, sourceKeyProperty, targetKeyProperty);
        }
    }

    private bool IsKnownEntityType(Type type)
    {
        return _knownEntityTypes.Contains(type) || _factoryMethods.ContainsKey(type) || _typeIdProxies.ContainsKey(type) || _typeConcurrencyTokenProxies.ContainsKey(type);
    }

    private void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context, ICustomPropertyMapperInternal? customPropertyMapper, bool isFirstRecursion)
    {
        if (!context.Contains(sourceType, targetType))
        {
            if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>()) == default)
            {
                throw new FactoryMethodException(targetType, true);
            }

            context.Push(sourceType, targetType);

            RegisterTypeKeyProxies(sourceType, targetType, context.MethodBuilder);

            var methodBuilder = context.MethodBuilder;
            var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance).ToList();
            var targetProperties = targetType.GetProperties(Utilities.PublicInstance).ToList();
            if (customPropertyMapper != null)
            {
                targetProperties = targetProperties.Except(customPropertyMapper.MappedTargetProperties).ToList();
            }

            (var sourceIdentityProperty, var targetIdentityProperty, var sourceConcurrencyTokenProperty, var targetConcurrencyTokenProperty) =
                ExtractKeyProperties(sourceType, targetType, sourceProperties, targetProperties);

            RegisterKeyComparer(KeyType.Id, _idComparers, sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, methodBuilder);
            RegisterKeyComparer(KeyType.ConcurrencyToken, _concurrencyTokenComparers, sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, methodBuilder);

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet?>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet?>();
                _mapper[sourceType] = innerMapper;
            }

            if (!innerMapper.TryGetValue(targetType, out var mapperMetaDataSet))
            {
                var keyPropertiesMapper = methodBuilder.BuildUpKeyPropertiesMapperMethod(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty);
                var mappedScalarProperties = ExtractScalarProperties(sourceProperties, targetProperties);
                var scalarPropertiesMapper = mappedScalarProperties.Any() ? methodBuilder.BuildUpScalarPropertiesMapperMethod(sourceType, targetType, mappedScalarProperties) : null;
                var mappedEntityProperties = ExtractEntityProperties(sourceProperties, targetProperties, context);
                var entityPropertiesMapper = mappedEntityProperties.Any() ? methodBuilder.BuildUpEntityPropertiesMapperMethod(sourceType, targetType, mappedEntityProperties) : null;
                var mappedEntityListProperties = ExtractEntityListProperties(sourceProperties, targetProperties, context);
                var entityListPropertiesMapper = mappedEntityListProperties.Any() ? methodBuilder.BuildUpEntityListPropertiesMapperMethod(sourceType, targetType, mappedEntityListProperties) : null;
                innerMapper[targetType] = Utilities.BuildMapperMetaDataSet(customPropertyMapper?.MapProperties, keyPropertiesMapper, scalarPropertiesMapper, entityPropertiesMapper, entityListPropertiesMapper);
            }
            else if (isFirstRecursion && customPropertyMapper != null)
            {
                innerMapper[targetType] = new MapperMetaDataSet(
                    customPropertyMapper.MapProperties,
                    mapperMetaDataSet?.keyPropertiesMapper,
                    mapperMetaDataSet?.scalarPropertiesMapper,
                    mapperMetaDataSet?.entityPropertiesMapper,
                    mapperMetaDataSet?.listPropertiesMapper);
            }

            context.Pop();
        }
        else
        {
            context.DumpLoopDependency();
        }
    }

    private (PropertyInfo?, PropertyInfo?, PropertyInfo?, PropertyInfo?) ExtractKeyProperties(Type sourceType, Type targetType, IList<PropertyInfo> sourceProperties, IList<PropertyInfo> targetProperties)
    {
        var sourceIdentityProperty = sourceProperties.GetKeyProperty(_keyPropertyNames.GetIdentityPropertyName(sourceType), false);
        var targetIdentityProperty = targetProperties.GetKeyProperty(_keyPropertyNames.GetIdentityPropertyName(targetType), true);
        var sourceConcurrencyTokenProperty = sourceProperties.GetKeyProperty(_keyPropertyNames.GetConcurrencyTokenPropertyName(sourceType), false);
        var targetConcurrencyTokenProperty = targetProperties.GetKeyProperty(_keyPropertyNames.GetConcurrencyTokenPropertyName(targetType), true);
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

    private IList<(PropertyInfo, Type, PropertyInfo, Type)> ExtractEntityProperties(IList<PropertyInfo> sourceProperties, IList<PropertyInfo> targetProperties, RecursiveRegisterContext context)
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
                context.RegisterIf(sourcePropertyType, targetPropertyType, _entityMapperTypeValidator.CanConvert(sourcePropertyType, targetPropertyType));
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

    private IList<(PropertyInfo, Type, PropertyInfo, Type)> ExtractEntityListProperties(IList<PropertyInfo> sourceProperties, IList<PropertyInfo> targetProperties, RecursiveRegisterContext context)
    {
        var sourceEntityListProperties = sourceProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(false));
        var targetEntityListProperties = targetProperties.Where(p => _entityListMapperTypeValidator.IsValidType(p.PropertyType) && p.VerifyGetterSetter(true)).ToDictionary(p => p.Name, p => p);
        var matchedProperties = new List<(PropertyInfo, Type, PropertyInfo, Type)>(sourceEntityListProperties.Count());
        foreach (var sourceProperty in sourceEntityListProperties)
        {
            if (targetEntityListProperties.TryGetValue(sourceProperty.Name, out var targetProperty))
            {
                var sourceItemType = sourceProperty.PropertyType.GetListItemType()!;
                var targetItemType = targetProperty.PropertyType.GetListItemType()!;

                // cascading mapper creation: if list item mapper doesn't exist, create it
                context.RegisterIf(sourceItemType!, targetItemType!, _entityListMapperTypeValidator.CanConvert(sourceItemType!, targetItemType!));
                matchedProperties.Add((sourceProperty, sourceItemType, targetProperty, targetItemType));
            }
        }

        return matchedProperties;
    }

    private void RegisterTypeKeyProxies(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder)
    {
        var sourceTypeRegistered = _keyPropertyConfiguration.ContainsKey(sourceType) || _typesUsingDefaultConfiguration.Contains(sourceType);
        var targetTypeRegistered = _keyPropertyConfiguration.ContainsKey(targetType) || _typesUsingDefaultConfiguration.Contains(targetType);
        if (!sourceTypeRegistered || !targetTypeRegistered)
        {
            var sourceIdProperty = sourceType.GetProperties(Utilities.PublicInstance).GetKeyProperty(_keyPropertyNames.GetIdentityPropertyName(sourceType), false);
            var targetIdProperty = targetType.GetProperties(Utilities.PublicInstance).GetKeyProperty(_keyPropertyNames.GetIdentityPropertyName(targetType), true);
            RegisterKeyProperty(sourceType, targetType, sourceIdProperty, targetIdProperty, sourceTypeRegistered, targetTypeRegistered, methodBuilder, _typeIdProxies, KeyType.Id);
            var sourceConcurrentyTokenProperty = sourceType.GetProperties(Utilities.PublicInstance).GetKeyProperty(_keyPropertyNames.GetConcurrencyTokenPropertyName(sourceType), false);
            var targetConcurrentyTokenProperty = targetType.GetProperties(Utilities.PublicInstance).GetKeyProperty(_keyPropertyNames.GetConcurrencyTokenPropertyName(targetType), true);
            RegisterKeyProperty(sourceType, targetType, sourceConcurrentyTokenProperty, targetConcurrentyTokenProperty, sourceTypeRegistered, targetTypeRegistered, methodBuilder, _typeConcurrencyTokenProxies, KeyType.ConcurrencyToken);
        }
    }

    private void RegisterKeyProperty(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceProperty,
        PropertyInfo? targetProperty,
        bool sourceTypeRegistered,
        bool targetTypeRegistered,
        IDynamicMethodBuilder methodBuilder,
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

        if (!sourceTypeRegistered)
        {
            _typesUsingDefaultConfiguration.Add(sourceType);
            if (sourcePropertyExists && !typeKeyProxies.ContainsKey(sourceType))
            {
                typeKeyProxies.Add(sourceType, BuildTypeKeyProxy(sourceType, sourceProperty!, methodBuilder, keyType));
            }
        }

        if (!targetTypeRegistered && sourceType != targetType)
        {
            _typesUsingDefaultConfiguration.Add(targetType);
            if (targetPropertyExists && !typeKeyProxies.ContainsKey(targetType))
            {
                typeKeyProxies.Add(targetType, BuildTypeKeyProxy(targetType, targetProperty!, methodBuilder, keyType));
            }
        }
    }
}

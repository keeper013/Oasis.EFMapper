namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class MapperRegistry
{
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarTypes = new ();
    private readonly HashSet<Type> _knownEntityTypes = new ();
    private readonly Dictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeIdProxies = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeConcurrencyTokenProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet?>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _idComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _concurrencyTokenComparers = new ();
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _typeListFactoryMethods = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping = new ();
    private readonly bool _defaultKeepEntityOnMappingRemoved;

    public MapperRegistry(TypeConfiguration defaultConfiguration)
    {
        _defaultKeepEntityOnMappingRemoved = defaultConfiguration.keepEntityOnMappingRemoved;
        KeyPropertyNames = new KeyPropertyNameManager(
            new KeyPropertyNameConfiguration(defaultConfiguration.identityPropertyName, defaultConfiguration.concurrencyTokenPropertyName),
            _typesUsingCustomConfiguration);
        ScalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTypes);
        EntityMapperTypeValidator = new EntityMapperTypeValidator(_mapper, _convertableToScalarTypes);
        EntityListMapperTypeValidator = new EntityListMapperTypeValidator(_mapper, _convertableToScalarTypes);
    }

    public IMapperTypeValidator ScalarMapperTypeValidator { get; }

    public IMapperTypeValidator EntityMapperTypeValidator { get; }

    public IMapperTypeValidator EntityListMapperTypeValidator { get; }

    public IKeyPropertyNameManager KeyPropertyNames { get; }

    public void Register(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder, ICustomPropertyMapperInternal? customPropertyMapper)
    {
        if (!EntityMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!EntityMapperTypeValidator.IsValidType(targetType))
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
        if (!EntityMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidEntityTypeException(sourceType);
        }

        if (!EntityMapperTypeValidator.IsValidType(targetType))
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

        if (!EntityMapperTypeValidator.IsValidType(itemType))
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

        if (!EntityMapperTypeValidator.IsValidType(type))
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

    public void WithConfiguration(Type type, TypeConfiguration configuration, IDynamicMethodBuilder methodBuilder, bool throwIfRedundant = false)
    {
        if (!EntityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        bool typeIsConfigurated = false;
        if (_typesUsingCustomConfiguration.ContainsKey(type))
        {
            typeIsConfigurated = true;
            if (throwIfRedundant)
            {
                throw new TypeConfiguratedException(type);
            }
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
            _typesUsingCustomConfiguration[type] = configuration;

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
        }
    }

    public void WithScalarConverter(Type sourceType, Type targetType, Delegate @delegate, bool throwIfRedundant = false)
    {
        var sourceIsScalarType = ScalarMapperTypeValidator.IsValidType(sourceType);
        var targetIsScalarType = ScalarMapperTypeValidator.IsValidType(targetType);
        if (!sourceIsScalarType && !targetIsScalarType)
        {
            throw new ScalarTypeMissingException(sourceType, targetType);
        }

        if (IsKnownEntityType(sourceType) || EntityListMapperTypeValidator.IsValidType(sourceType))
        {
            throw new InvalidScalarTypeException(sourceType);
        }

        if (IsKnownEntityType(targetType) || EntityListMapperTypeValidator.IsValidType(targetType))
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
        var customKeepEntityOnRemoval = _typesUsingCustomConfiguration
            .Where(pair => pair.Value.keepEntityOnMappingRemoved != IMapperBuilder.DefaultKeepEntityOnMappingRemoved)
            .ToDictionary(pair => pair.Key, pair => pair.Value.keepEntityOnMappingRemoved);
        return new EntityBaseProxy(_typeIdProxies, _typeConcurrencyTokenProxies, _idComparers, _concurrencyTokenComparers, customKeepEntityOnRemoval, type, scalarTypeConverter, _defaultKeepEntityOnMappingRemoved);
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
        _typesUsingCustomConfiguration.Clear();
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
        Func<Type, string?> getPropertyName,
        Type sourceType,
        Type targetType,
        PropertyInfo[] sourceProperties,
        PropertyInfo[] targetProperties,
        IDynamicMethodBuilder methodBuilder)
    {
        if (!comparers.TryGetValue(sourceType, out Dictionary<Type, MethodMetaData>? innerComparer))
        {
            innerComparer = new Dictionary<Type, MethodMetaData>();
            comparers[sourceType] = innerComparer;
        }

        if (!innerComparer.ContainsKey(targetType))
        {
            var sourcePropertyName = getPropertyName(sourceType);
            var targetPropertyName = getPropertyName(targetType);
            var sourceKeyProperty = sourceProperties.FirstOrDefault(p => string.Equals(p.Name, sourcePropertyName));
            var targetKeyProperty = targetProperties.FirstOrDefault(p => string.Equals(p.Name, targetPropertyName));
            if (sourceKeyProperty != default && targetKeyProperty != default)
            {
                innerComparer![targetType] = methodBuilder.BuildUpKeyEqualComparerMethod(keyType, sourceType, targetType, sourceKeyProperty, targetKeyProperty);
            }
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
            var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
            var targetProperties = targetType.GetProperties(Utilities.PublicInstance);
            if (customPropertyMapper != null)
            {
                targetProperties = targetProperties.Except(customPropertyMapper.MappedTargetProperties).ToArray();
            }

            RegisterKeyComparer(KeyType.Id, _idComparers, KeyPropertyNames.GetIdentityPropertyName, sourceType, targetType, sourceProperties, targetProperties, methodBuilder);
            RegisterKeyComparer(KeyType.ConcurrencyToken, _concurrencyTokenComparers, KeyPropertyNames.GetConcurrencyTokenPropertyName, sourceType, targetType, sourceProperties, targetProperties, methodBuilder);

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet?>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet?>();
                _mapper[sourceType] = innerMapper;
            }

            if (!innerMapper.TryGetValue(targetType, out var mapperMetaDataSet))
            {
                innerMapper[targetType] = Utilities.BuildMapperMetaDataSet(
                    customPropertyMapper?.MapProperties,
                    methodBuilder.BuildUpKeyPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
                    methodBuilder.BuildUpScalarPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
                    methodBuilder.BuildUpEntityPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context),
                    methodBuilder.BuildUpEntityListPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context));
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

    private void RegisterTypeKeyProxies(Type sourceType, Type targetType, IDynamicMethodBuilder methodBuilder)
    {
        var sourceTypeRegistered = _typesUsingCustomConfiguration.ContainsKey(sourceType) || _typesUsingDefaultConfiguration.Contains(sourceType);
        var targetTypeRegistered = _typesUsingCustomConfiguration.ContainsKey(targetType) || _typesUsingDefaultConfiguration.Contains(targetType);
        if (!sourceTypeRegistered || !targetTypeRegistered)
        {
            var sourceIdProperty = sourceType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetIdentityPropertyName(sourceType), false);
            var targetIdProperty = targetType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetIdentityPropertyName(targetType), true);
            RegisterKeyProperty(sourceType, targetType, sourceIdProperty, targetIdProperty, sourceTypeRegistered, targetTypeRegistered, methodBuilder, _typeIdProxies, KeyType.Id);
            var sourceConcurrentyTokenProperty = sourceType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetConcurrencyTokenPropertyName(sourceType), false);
            var targetConcurrentyTokenProperty = targetType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetConcurrencyTokenPropertyName(targetType), true);
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
            if (sourceIdType != targetIdType && !ScalarMapperTypeValidator.CanConvert(sourceIdType, targetIdType))
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

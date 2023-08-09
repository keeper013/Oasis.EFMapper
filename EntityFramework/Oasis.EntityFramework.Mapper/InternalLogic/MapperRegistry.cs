namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Reflection.Emit;
using Oasis.EntityFramework.Mapper.Exceptions;

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
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _entityListFactoryMethods = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping = new ();
    private readonly Dictionary<Type, ISet<string>> _dependentPropertiesDictionary = new ();
    private readonly Dictionary<Type, ExistingTargetTrackerMetaDataSet> _existingTargetTrackers = new ();
    private readonly Dictionary<Type, MethodMetaData> _entityDefaultConstructors = new ();
    private readonly Dictionary<Type, MethodMetaData> _entityListDefaultConstructors = new ();
    private readonly HashSet<Type> _targetsToBeTracked = new ();
    private readonly IMapperTypeValidator _scalarMapperTypeValidator;
    private readonly IMapperTypeValidator _entityMapperTypeValidator;
    private readonly KeyPropertyNameManager _keyPropertyNames;
    private readonly ExcludedPropertyManager _excludedPropertyManager;
    private readonly MapToDatabaseType _defaultMapToDatabase;

    public MapperRegistry(ModuleBuilder module, IMapperBuilderConfiguration? configuration)
    {
        _defaultMapToDatabase = configuration?.MapToDatabaseType ?? MapToDatabaseType.Upsert;
        _keyPropertyNames = new (new KeyPropertyNameConfiguration(configuration?.IdentityPropertyName, configuration?.ConcurrencyTokenPropertyName));
        _scalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTypes);
        _entityMapperTypeValidator = new EntityMapperTypeValidator(_mapper);
        _excludedPropertyManager = new (configuration?.ExcludedProperties);
        _dynamicMethodBuilder = new (
            module.DefineType("Mapper", TypeAttributes.Public),
            _scalarMapperTypeValidator,
            _entityMapperTypeValidator,
            new EntityListMapperTypeValidator(_mapper));
    }

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

        _toBeRegistered.AddOrUpdateNull(sourceType, targetType, configuration);
    }

    public void Configure(Type type, IEntityConfiguration configuration)
    {
        if (!_entityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        var properties = type.GetProperties(Utilities.PublicInstance);
        if (!string.IsNullOrEmpty(configuration.IdentityPropertyName))
        {
            var identityProperty = properties.GetKeyProperty(configuration.IdentityPropertyName, false);
            if (identityProperty != default)
            {
                _keyPropertyNames.Add(type, new KeyPropertyConfiguration(configuration.IdentityPropertyName!, configuration.ConcurrencyTokenPropertyName));
            }
            else
            {
                throw new MissingKeyPropertyException(type, "identity", configuration.IdentityPropertyName!);
            }

            if (!string.IsNullOrEmpty(configuration.ConcurrencyTokenPropertyName) && properties.GetKeyProperty(configuration.IdentityPropertyName, false) == null)
            {
                throw new MissingKeyPropertyException(type, "concurrency token", configuration.ConcurrencyTokenPropertyName!);
            }
        }

        if (configuration.DependentProperties != null)
        {
            _dependentPropertiesDictionary.Add(type, configuration.DependentProperties);
        }

        if (configuration.ExcludedProperties != null && configuration.ExcludedProperties.Any())
        {
            foreach (var propertyName in configuration.ExcludedProperties)
            {
                if (!properties.Any(p => string.Equals(p.Name, propertyName)))
                {
                    throw new UselessExcludeException(type, propertyName);
                }
            }

            _excludedPropertyManager.Add(type, configuration.ExcludedProperties);
        }
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
        || _excludedPropertyManager.ContainsTypeConfiguration(entityType);

    public IScalarTypeConverter MakeScalarTypeConverter()
    {
        return new ScalarTypeConverter(_scalarConverterDictionary);
    }

    public IListTypeConstructor MakeListTypeConstructor(Type type)
    {
        return new ListTypeConstructor(_entityListFactoryMethods, _entityListDefaultConstructors, type);
    }

    public MapperSetLookUp MakeMapperSetLookUp(Type type)
    {
        return new MapperSetLookUp(_mapper, type);
    }

    public ExistingTargetTrackerFactory MakeExistingTargetTrackerFactory(Type type)
    {
        return new ExistingTargetTrackerFactory(_existingTargetTrackers, _targetsToBeTracked, type);
    }

    public EntityBaseProxy MakeEntityBaseProxy(Type type, IScalarTypeConverter scalarTypeConverter)
    {
        return new EntityBaseProxy(_typeIdProxies, _typeConcurrencyTokenProxies, _idComparers, _concurrencyTokenComparers, _sourceIdEqualsTargetId, _sourceIdListContainsTargetId, type, scalarTypeConverter);
    }

    public IEntityFactory MakeEntityFactory(Type type)
    {
        return new EntityFactory(_factoryMethods, _entityDefaultConstructors, type);
    }

    public NewTargetTrackerProvider MakeNewTargetTrackerProvider(IEntityFactory entityFactory)
    {
        return new NewTargetTrackerProvider(_loopDependencyMapping, entityFactory);
    }

    public DependentPropertyManager MakeDependentPropertyManager()
    {
        return new DependentPropertyManager(_dependentPropertiesDictionary);
    }

    public MapToDatabaseTypeManager MakeMapToDatabaseTypeManager()
    {
        return new MapToDatabaseTypeManager(_defaultMapToDatabase, _mapToDatabaseDictionary);
    }

    public void RegisterEntityListDefaultConstructorMethod(Type listType)
    {
        if (listType.IsConstructable() && !listType.IsList() && !_entityListDefaultConstructors.ContainsKey(listType) && !_entityListFactoryMethods.ContainsKey(listType))
        {
            _entityListDefaultConstructors.Add(listType, _dynamicMethodBuilder.BuildUpConstructorMethod(listType));
        }
    }

    public void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context, RecursivelyRegisterType recursivelyRegisterType)
    {
        if (!context.Contains(sourceType, targetType))
        {
            if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Utilities.PublicInstance, null, Array.Empty<Type>(), null) == default)
            {
                throw new FactoryMethodException(targetType, true);
            }

            RegisterEntityDefaultConstructorMethod(targetType);

            var configuration = _toBeRegistered.Pop(sourceType, targetType);

            using var ctx = new RecursiveContextPopper(context, sourceType, targetType);
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

            if (recursivelyRegisterType == RecursivelyRegisterType.ListOfEntityProperty)
            {
                RegisterEntityListProperty(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty);
            }

            RegisterSourceIdEqualsTargetIdMethod(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty);
            RegisterKeyProperty(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder, _typeIdProxies, KeyType.Id);
            RegisterKeyProperty(sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder, _typeConcurrencyTokenProxies, KeyType.ConcurrencyToken);
            RegisterKeyComparer(KeyType.Id, _idComparers, sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder);
            RegisterKeyComparer(KeyType.ConcurrencyToken, _concurrencyTokenComparers, sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder);
            RegisterExistingTargetTracker(targetType, targetIdentityProperty, context);

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
        else
        {
            if (recursivelyRegisterType == RecursivelyRegisterType.ListOfEntityProperty)
            {
                var (sourceIdentityProperty, targetIdentityProperty) = GetIdentityProperties(sourceType, targetType);
                RegisterEntityListProperty(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty);
            }

            context.DumpLoopDependency();
        }
    }

    public void Clear()
    {
        _scalarConverterDictionary.Clear();
        _keyPropertyNames.Clear();
        _typeIdProxies.Clear();
        _typeConcurrencyTokenProxies.Clear();
        _mapper.Clear();
        _idComparers.Clear();
        _concurrencyTokenComparers.Clear();
        _existingTargetTrackers.Clear();
        _entityDefaultConstructors.Clear();
        _entityListDefaultConstructors.Clear();
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

        RecursivelyRegister(sourceType, targetType, new RecursiveRegisterContext(_loopDependencyMapping, _targetsToBeTracked), RecursivelyRegisterType.TopLevel);
    }

    private void RegisterEntityDefaultConstructorMethod(Type type)
    {
        if (!_entityDefaultConstructors.ContainsKey(type) && !_factoryMethods.ContainsKey(type))
        {
            _entityDefaultConstructors.Add(type, _dynamicMethodBuilder.BuildUpConstructorMethod(type));
        }
    }

    private (PropertyInfo?, PropertyInfo?) GetIdentityProperties(Type sourceType, Type targetType)
    {
        var sourceIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(sourceType);
        var sourceIdentityProperty = string.IsNullOrEmpty(sourceIdentityPropertyName) ? default : sourceType.GetProperty(sourceIdentityPropertyName, Utilities.PublicInstance);
        if (sourceIdentityProperty != default && !sourceIdentityProperty.VerifyGetterSetter(false))
        {
            sourceIdentityProperty = default;
        }

        var targetIdentityPropertyName = _keyPropertyNames.GetIdentityPropertyName(targetType);
        var targetIdentityProperty = string.IsNullOrEmpty(targetIdentityPropertyName) ? default : targetType.GetProperty(targetIdentityPropertyName, Utilities.PublicInstance);
        if (targetIdentityProperty != default && !targetIdentityProperty.VerifyGetterSetter(true))
        {
            targetIdentityProperty = default;
        }

        return (sourceIdentityProperty, targetIdentityProperty);
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

    private void RegisterEntityListProperty(Type sourceType, PropertyInfo? sourceIdentityProperty, Type targetType, PropertyInfo? targetIdentityProperty)
    {
        if (sourceIdentityProperty != null && targetIdentityProperty != null)
        {
            _sourceIdListContainsTargetId.AddIfNotExists(
                sourceType,
                targetType,
                () => _dynamicMethodBuilder.BuildUpSourceIdListContainsTargetIdMethod(sourceType, sourceIdentityProperty, targetType, targetIdentityProperty));
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

    private void RegisterExistingTargetTracker(Type targetType, PropertyInfo? targetIdentityProperty, RecursiveRegisterContext context)
    {
        if (targetIdentityProperty != default && !_existingTargetTrackers.ContainsKey(targetType))
        {
            _existingTargetTrackers.Add(
                targetType,
                new ExistingTargetTrackerMetaDataSet(
                    _dynamicMethodBuilder.BuildUpBuildExistingTargetTrackerMethod(targetType, targetIdentityProperty),
                    _dynamicMethodBuilder.BuildUpStartTrackExistingTargetMethod(targetType, targetIdentityProperty)));
            context.DumpTargetsToBeTracked();
        }
    }
}

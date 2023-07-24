namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection.Emit;
using System.Security.AccessControl;
using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal record struct KeyPropertyConfiguration(string identityPropertyName, string? concurrencyTokenPropertyName = default);

internal sealed class MapperRegistry : IRecursiveRegister
{
    private readonly IDynamicMethodBuilder _dynamicMethodBuilder;
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarTypes = new ();
    private readonly HashSet<Type> _knownEntityTypes = new ();
    private readonly Dictionary<Type, bool> _typeKeepEntityOnMappingRemovedConfiguration = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeIdProxies = new ();
    private readonly Dictionary<Type, TypeKeyProxyMetaDataSet> _typeConcurrencyTokenProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet?>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _idComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, MethodMetaData>> _concurrencyTokenComparers = new ();
    private readonly Dictionary<Type, Dictionary<Type, bool>> _mappingKeepEntityOnMappingRemoved = new ();
    private readonly Dictionary<Type, Dictionary<Type, IReadOnlyDictionary<string, bool>>> _propertyKeepEntityOnMappingRemoved = new ();
    private readonly Dictionary<Type, Dictionary<Type, ICustomTypeMapperConfiguration?>> _toBeRegistered = new ();
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _typeListFactoryMethods = new ();
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping = new ();
    private readonly IMapperTypeValidator _scalarMapperTypeValidator;
    private readonly IMapperTypeValidator _entityMapperTypeValidator;
    private readonly KeyPropertyNameManager _keyPropertyNames;
    private readonly ExcludedPropertyManager _excludedPropertyManager = new ();
    private readonly bool _defaultKeepEntityOnMappingRemoved;

    public MapperRegistry(ModuleBuilder module, EntityConfiguration defaultConfiguration)
    {
        _keyPropertyNames = new KeyPropertyNameManager(new KeyPropertyNameConfiguration(defaultConfiguration.identityPropertyName, defaultConfiguration.concurrencyTokenPropertyName));
        _scalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTypes);
        _entityMapperTypeValidator = new EntityMapperTypeValidator(_mapper);
        _defaultKeepEntityOnMappingRemoved = defaultConfiguration.keepEntityOnMappingRemoved ?? IMapperBuilder.DefaultKeepEntityOnMappingRemoved;
        _dynamicMethodBuilder = new DynamicMethodBuilder(
            module.DefineType("Mapper", TypeAttributes.Public),
            _scalarMapperTypeValidator,
            _entityMapperTypeValidator,
            new EntityListMapperTypeValidator(_mapper));
    }

    public void Register(Type sourceType, Type targetType, ICustomTypeMapperConfiguration? configuration)
    {
        if (configuration != null)
        {
            if (configuration.CustomPropertyMapper == null && configuration.PropertyEntityRemover == null && configuration.ExcludedProperties == null)
            {
                throw new ArgumentException($"At least 1 configuration item of {nameof(ICustomTypeMapperConfiguration)} should not be null.", nameof(configuration));
            }

            if (configuration.ExcludedProperties != null)
            {
                if (configuration.CustomPropertyMapper != null)
                {
                    var excluded = configuration.CustomPropertyMapper.MappedTargetProperties.FirstOrDefault(p => configuration.ExcludedProperties.Contains(p.Name));
                    if (excluded != null)
                    {
                        throw new CustomMappingPropertyExcludedException(sourceType, targetType, excluded.Name);
                    }
                }

                var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
                var targetProperties = targetType.GetProperties(Utilities.PublicInstance);
                foreach (var propertyName in configuration.ExcludedProperties)
                {
                    if (!sourceProperties.Any(p => string.Equals(p.Name, propertyName)) || !targetProperties.Any(p => string.Equals(p.Name, propertyName)))
                    {
                        throw new UselessExcludeException(sourceType, targetType, propertyName);
                    }
                }

                _excludedPropertyManager.Add(sourceType, targetType, new HashSet<string>(configuration.ExcludedProperties));
            }
        }

        _toBeRegistered.Add(sourceType, targetType, configuration);
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

    public void WithConfiguration(Type type, EntityConfiguration configuration, bool throwIfRedundant = false)
    {
        if (!_entityMapperTypeValidator.IsValidType(type))
        {
            throw new InvalidEntityTypeException(type);
        }

        bool typeIsConfigurated = false;
        if (_keyPropertyNames.ContainsConfiguration(type) || _typeKeepEntityOnMappingRemovedConfiguration.ContainsKey(type)
            || _excludedPropertyManager.ContainsTypeConfiguration(type))
        {
            if (throwIfRedundant)
            {
                throw new TypeConfiguratedException(type);
            }

            typeIsConfigurated = true;
        }

        if (!typeIsConfigurated)
        {
            var properties = type.GetProperties(Utilities.PublicInstance);
            var identityProperty = properties.GetKeyProperty(configuration.identityPropertyName, false);
            if (identityProperty != default)
            {
                _keyPropertyNames.Add(type, new KeyPropertyConfiguration(configuration.identityPropertyName, configuration.concurrencyTokenPropertyName));
            }

            if (configuration.keepEntityOnMappingRemoved.HasValue)
            {
                if (identityProperty == null)
                {
                    throw new MissingIdentityException(type);
                }

                _typeKeepEntityOnMappingRemovedConfiguration.Add(type, configuration.keepEntityOnMappingRemoved.Value);
            }

            if (configuration.excludedProperties != null && configuration.excludedProperties.Any())
            {
                foreach (var propertyName in configuration.excludedProperties)
                {
                    if (!properties.Any(p => string.Equals(p.Name, propertyName)))
                    {
                        throw new UselessExcludeException(type, propertyName);
                    }
                }

                _excludedPropertyManager.Add(type, new HashSet<string>(configuration.excludedProperties));
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

    public EntityRemover MakeEntityRemover()
    {
        return new EntityRemover(_defaultKeepEntityOnMappingRemoved, _typeKeepEntityOnMappingRemovedConfiguration, _mappingKeepEntityOnMappingRemoved, _propertyKeepEntityOnMappingRemoved);
    }

    public void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context)
    {
        if (!context.Contains(sourceType, targetType))
        {
            if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>()) == default)
            {
                throw new FactoryMethodException(targetType, true);
            }

            var configuration = _toBeRegistered.Pop(sourceType, targetType);

            using var ctx = new RecursiveContextPopper(context, sourceType, targetType);
            (var sourceExcludedProperties, var targetExcludedProperties) = _excludedPropertyManager.GetExcludedPropertyNames(sourceType, targetType);
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

            (var sourceIdentityProperty, var targetIdentityProperty, var sourceConcurrencyTokenProperty, var targetConcurrencyTokenProperty) =
                ExtractKeyProperties(sourceType, targetType, sourceProperties, targetProperties, sourceExcludedProperties, targetExcludedProperties);

            RegisterKeyProperty(sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder, _typeIdProxies, KeyType.Id);
            RegisterKeyProperty(sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder, _typeConcurrencyTokenProxies, KeyType.ConcurrencyToken);
            RegisterKeyComparer(KeyType.Id, _idComparers, sourceType, targetType, sourceIdentityProperty, targetIdentityProperty, _dynamicMethodBuilder);
            RegisterKeyComparer(KeyType.ConcurrencyToken, _concurrencyTokenComparers, sourceType, targetType, sourceConcurrencyTokenProperty, targetConcurrencyTokenProperty, _dynamicMethodBuilder);

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet?>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet?>();
                _mapper[sourceType] = innerMapper;
            }

            if (!innerMapper.TryGetValue(targetType, out var mapperMetaDataSet))
            {
                ISet<string>? keepEntityOnMappingRemovedProperties = null;
                if (configuration?.PropertyEntityRemover != null)
                {
                    var remover = configuration.PropertyEntityRemover;
                    if (remover.MappingKeepEntityOnMappingRemoved.HasValue)
                    {
                        _mappingKeepEntityOnMappingRemoved.Add(sourceType, targetType, remover.MappingKeepEntityOnMappingRemoved.Value);
                    }

                    if (remover.PropertyKeepEntityOnMappingRemoved != null)
                    {
                        _propertyKeepEntityOnMappingRemoved.Add(sourceType, targetType, remover.PropertyKeepEntityOnMappingRemoved);
                        keepEntityOnMappingRemovedProperties = remover.PropertyKeepEntityOnMappingRemoved.Keys.ToHashSet();
                    }
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
                    context,
                    keepEntityOnMappingRemovedProperties);
                innerMapper[targetType] = Utilities.BuildMapperMetaDataSet(configuration?.CustomPropertyMapper?.MapProperties, keyMapper, contentMapper);
            }
        }
        else
        {
            context.DumpLoopDependency();
        }
    }

    public void Clear()
    {
        _scalarConverterDictionary.Clear();
        _convertableToScalarTypes.Clear();
        _knownEntityTypes.Clear();
        _keyPropertyNames.Clear();
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
        comparers.Add(
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

    private void RegisterKeyProperty(
        Type sourceType,
        Type targetType,
        PropertyInfo? sourceProperty,
        PropertyInfo? targetProperty,
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

        if (sourcePropertyExists && !typeKeyProxies.ContainsKey(sourceType))
        {
            typeKeyProxies.Add(sourceType, BuildTypeKeyProxy(sourceType, sourceProperty!, methodBuilder, keyType));
        }

        if (targetPropertyExists && !typeKeyProxies.ContainsKey(targetType))
        {
            typeKeyProxies.Add(targetType, BuildTypeKeyProxy(targetType, targetProperty!, methodBuilder, keyType));
        }
    }
}

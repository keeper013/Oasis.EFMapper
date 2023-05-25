namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;

internal sealed class MapperRegistry
{
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarTypes = new ();
    private readonly HashSet<Type> _knownEntityTypes = new ();
    private readonly Dictionary<Type, TypeConfiguration> _typesUsingCustomConfiguration = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();
    private readonly Dictionary<Type, TypeProxyMetaDataSet> _typeProxies = new ();
    private readonly Dictionary<Type, Dictionary<Type, MapperMetaDataSet>> _mapper = new ();
    private readonly Dictionary<Type, Dictionary<Type, ComparerMetaDataSet>> _comparer = new ();
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly Dictionary<Type, Delegate> _typeListFactoryMethods = new ();
    private readonly bool _defaultKeepEntityOnMappingRemoved;

    public MapperRegistry(TypeConfiguration defaultConfiguration)
    {
        _defaultKeepEntityOnMappingRemoved = defaultConfiguration.keepEntityOnMappingRemoved;
        KeyPropertyNames = new KeyPropertyNameManager(
            new KeyPropertyNameConfiguration(defaultConfiguration.identityPropertyName, defaultConfiguration.timestampPropertyName),
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

        RecursivelyRegister(sourceType, targetType, new RecursiveRegisterContext(this, methodBuilder), customPropertyMapper);
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

        var context = new RecursiveRegisterContext(this, methodBuilder);
        RecursivelyRegister(sourceType, targetType, context, customPropertyMapperSourceToTarget);
        RecursivelyRegister(targetType, sourceType, context, customPropertyMapperTargetToSource);
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
                _typeProxies.Add(type, BuildTypeProxy(type, identityProperty, methodBuilder, configuration.keepEntityOnMappingRemoved));
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
        return new EntityBaseProxy(_typeProxies, _comparer, type, scalarTypeConverter, _defaultKeepEntityOnMappingRemoved);
    }

    public EntityFactory MakeEntityFactory()
    {
        return new EntityFactory(_factoryMethods);
    }

    public void Clear()
    {
        _scalarConverterDictionary.Clear();
        _convertableToScalarTypes.Clear();
        _knownEntityTypes.Clear();
        _typesUsingCustomConfiguration.Clear();
        _typesUsingDefaultConfiguration.Clear();
        _typeProxies.Clear();
        _mapper.Clear();
        _comparer.Clear();
    }

    private bool IsKnownEntityType(Type type)
    {
        return _knownEntityTypes.Contains(type) || _factoryMethods.ContainsKey(type) || _typeProxies.ContainsKey(type);
    }

    private void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context, ICustomPropertyMapperInternal? customPropertyMapper)
    {
        if (!context.Contains(sourceType, targetType))
        {
            if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Utilities.PublicInstance, null, new Type[0], null) == default)
            {
                throw new FactoryMethodException(targetType, true);
            }

            context.Push(sourceType, targetType);

            RegisterTypeProxies(sourceType, targetType, context.MethodBuilder);

            var methodBuilder = context.MethodBuilder;
            var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
            var targetProperties = targetType.GetProperties(Utilities.PublicInstance);
            if (customPropertyMapper != null)
            {
                targetProperties = targetProperties.Except(customPropertyMapper.MappedTargetProperties).ToArray();
            }

            if (!_comparer.TryGetValue(sourceType, out Dictionary<Type, ComparerMetaDataSet>? innerComparer))
            {
                innerComparer = new Dictionary<Type, ComparerMetaDataSet>();
                _comparer[sourceType] = innerComparer;
            }

            if (!innerComparer.ContainsKey(targetType))
            {
                var sourceIdProperty = sourceProperties.FirstOrDefault(p => string.Equals(p.Name, KeyPropertyNames.GetIdentityPropertyName(sourceType)));
                var targetIdProperty = targetProperties.FirstOrDefault(p => string.Equals(p.Name, KeyPropertyNames.GetIdentityPropertyName(targetType)));
                if (sourceIdProperty != default && targetIdProperty != default)
                {
                    innerComparer![targetType] = new ComparerMetaDataSet(
                        methodBuilder.BuildUpIdEqualComparerMethod(sourceType, targetType, sourceIdProperty, targetIdProperty));
                }
            }

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet>();
                _mapper[sourceType] = innerMapper;
            }

            if (!innerMapper.ContainsKey(targetType))
            {
                innerMapper[targetType] = new MapperMetaDataSet(
                    customPropertyMapper?.MapProperties,
                    methodBuilder.BuildUpKeyPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
                    methodBuilder.BuildUpScalarPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
                    methodBuilder.BuildUpEntityPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context),
                    methodBuilder.BuildUpEntityListPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties, context));
            }
            else if (customPropertyMapper != null)
            {
                throw new MapperExistsException(sourceType.Name, targetType.Name);
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
            var sourceIdProperty = sourceType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetIdentityPropertyName(sourceType), false);
            var targetIdProperty = targetType.GetProperties(Utilities.PublicInstance).GetKeyProperty(KeyPropertyNames.GetIdentityPropertyName(targetType), true);

            var sourceHasId = sourceIdProperty != default;
            var targetHasId = targetIdProperty != default;
            if (sourceHasId && targetHasId)
            {
                var sourceIdType = sourceIdProperty!.PropertyType;
                var targetIdType = targetIdProperty!.PropertyType;
                if (sourceIdType != targetIdType && !ScalarMapperTypeValidator.CanConvert(sourceIdType, targetIdType))
                {
                    throw new ScalarConverterMissingException(sourceIdType, targetIdType);
                }
            }

            if (!sourceTypeRegistered)
            {
                _typesUsingDefaultConfiguration.Add(sourceType);
                if (sourceHasId)
                {
                    _typeProxies.Add(sourceType, BuildTypeProxy(sourceType, sourceIdProperty!, methodBuilder));
                }
            }

            if (!targetTypeRegistered && sourceType != targetType)
            {
                _typesUsingDefaultConfiguration.Add(targetType);
                if (targetHasId)
                {
                    _typeProxies.Add(targetType, BuildTypeProxy(targetType, targetIdProperty!, methodBuilder));
                }
            }
        }
    }

    private TypeProxyMetaDataSet BuildTypeProxy(
        Type type,
        PropertyInfo identityProperty,
        IDynamicMethodBuilder methodBuilder,
        bool keepEntityOnMappingRemoved = false)
    {
        return new TypeProxyMetaDataSet(
            methodBuilder.BuildUpGetIdMethod(type, identityProperty),
            methodBuilder.BuildUpIdIsEmptyMethod(type, identityProperty),
            identityProperty,
            keepEntityOnMappingRemoved);
    }
}

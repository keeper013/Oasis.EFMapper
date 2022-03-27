namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;
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
    private readonly Dictionary<Type, Delegate> _factoryMethods = new ();
    private readonly bool _defaultKeepEntityOnMappingRemoved;

    public MapperRegistry(TypeConfiguration defaultConfiguration)
    {
        _defaultKeepEntityOnMappingRemoved = defaultConfiguration.keepEntityOnMappingRemoved;
        KeyPropertyNames = new KeyPropertyNameManager(
            new KeyPropertyNameConfiguration(defaultConfiguration.identityPropertyName, defaultConfiguration.timestampPropertyName),
            _typesUsingCustomConfiguration);
        ScalarMapperTypeValidator = new ScalarMapperTypeValidator(_scalarConverterDictionary, _convertableToScalarTargetTypes);
        EntityMapperTypeValidator = new EntityMapperTypeValidator(_mapper, _convertableToScalarSourceTypes, _convertableToScalarTargetTypes);
        EntityListMapperTypeValidator = new EntityListMapperTypeValidator(_mapper, _convertableToScalarSourceTypes, _convertableToScalarTargetTypes);
    }

    public IMapperTypeValidator ScalarMapperTypeValidator { get; }

    public IMapperTypeValidator EntityMapperTypeValidator { get; }

    public IMapperTypeValidator EntityListMapperTypeValidator { get; }

    public IKeyPropertyNameManager KeyPropertyNames { get; }

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

    public void WithFactoryMethod(Type type, Delegate factoryMethod, bool throwIfRedundant = false)
    {
        if (factoryMethod == null)
        {
            throw new ArgumentNullException(nameof(factoryMethod));
        }

        if (type.GetConstructor(Array.Empty<Type>()) != null)
        {
            throw new FactoryMethodException(type, false);
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
        if (!EntityMapperTypeValidator.IsSourceType(type) && !EntityMapperTypeValidator.IsTargetType(type))
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

            var identityProperty = type.GetProperties(Utilities.PublicInstance).GetProperty(configuration.identityPropertyName);
            _typeProxies.Add(type, BuildTypeProxy(type, identityProperty, methodBuilder, configuration.keepEntityOnMappingRemoved));
        }
    }

    public void WithScalarConverter(Type sourceType, Type targetType, Delegate @delegate, bool throwIfRedundant = false)
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
        else if (throwIfRedundant)
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
        return new EntityBaseProxy(_typeProxies, _comparer, type, scalarTypeConverter, _defaultKeepEntityOnMappingRemoved);
    }

    public EntityFactory MakeEntityFactory()
    {
        return new EntityFactory(_factoryMethods);
    }

    private void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context)
    {
        if (!context.Contains(sourceType, targetType))
        {
            if (!_factoryMethods.ContainsKey(targetType) && targetType.GetConstructor(Array.Empty<Type>()) == null)
            {
                throw new FactoryMethodException(targetType, true);
            }

            context.Push(sourceType, targetType);

            RegisterTypeProxies(sourceType, targetType, context.MethodBuilder);

            if (!_comparer.TryGetValue(sourceType, out Dictionary<Type, ComparerMetaDataSet>? innerComparer))
            {
                innerComparer = new Dictionary<Type, ComparerMetaDataSet>();
                _comparer[sourceType] = innerComparer;
            }

            if (!_mapper.TryGetValue(sourceType, out Dictionary<Type, MapperMetaDataSet>? innerMapper))
            {
                innerMapper = new Dictionary<Type, MapperMetaDataSet>();
                _mapper[sourceType] = innerMapper;
            }

            var methodBuilder = context.MethodBuilder;
            var sourceProperties = sourceType.GetProperties(Utilities.PublicInstance);
            var targetProperties = targetType.GetProperties(Utilities.PublicInstance);

            if (!innerComparer.ContainsKey(targetType))
            {
                var sourceIdProperty = sourceProperties.First(p => string.Equals(p.Name, KeyPropertyNames.GetIdentityPropertyName(sourceType)));
                var targetIdProperty = sourceProperties.First(p => string.Equals(p.Name, KeyPropertyNames.GetIdentityPropertyName(targetType)));
                innerComparer![targetType] = new ComparerMetaDataSet(
                    methodBuilder.BuildUpIdEqualComparerMethod(sourceType, targetType, sourceIdProperty, targetIdProperty));
            }

            if (!innerMapper.ContainsKey(targetType))
            {
                innerMapper[targetType] = new MapperMetaDataSet(
                    methodBuilder.BuildUpKeyPropertiesMapperMethod(sourceType, targetType, sourceProperties, targetProperties),
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
            var sourceIdProperty = sourceType.GetProperties(Utilities.PublicInstance).GetProperty(KeyPropertyNames.GetIdentityPropertyName(sourceType));
            var targetIdProperty = targetType.GetProperties(Utilities.PublicInstance).GetProperty(KeyPropertyNames.GetIdentityPropertyName(targetType));

            var sourceIdType = sourceIdProperty.PropertyType;
            var targetIdType = targetIdProperty.PropertyType;
            if (sourceIdType != targetIdType && !ScalarMapperTypeValidator.CanConvert(sourceIdType, targetIdType))
            {
                throw new ScalarConverterMissingException(sourceIdType, targetIdType);
            }

            if (!sourceTypeRegistered)
            {
                _typesUsingDefaultConfiguration.Add(sourceType);
                _typeProxies.Add(sourceType, BuildTypeProxy(sourceType, sourceIdProperty, methodBuilder));
            }

            if (!targetTypeRegistered && sourceType != targetType)
            {
                _typesUsingDefaultConfiguration.Add(targetType);
                _typeProxies.Add(targetType, BuildTypeProxy(targetType, targetIdProperty, methodBuilder));
            }
        }
    }

    private TypeProxyMetaDataSet BuildTypeProxy(
        Type type,
        PropertyInfo identityProperty,
        IDynamicMethodBuilder methodBuilder,
        bool keepEntityOnMappingRemoved = IMapperBuilder.DefaultKeepEntityOnMappingRemoved)
    {
        return new TypeProxyMetaDataSet(
            methodBuilder.BuildUpGetIdMethod(type, identityProperty),
            methodBuilder.BuildUpIdIsEmptyMethod(type, identityProperty),
            identityProperty,
            keepEntityOnMappingRemoved);
    }
}

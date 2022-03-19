namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class TypeConfigurationCache
{
    private readonly ScalarConverterCache _scalarConverterCache;
    private readonly Dictionary<Type, TypeConfiguration> _typeConfigurations = new ();
    private readonly HashSet<Type> _typesUsingDefaultConfiguration = new ();

    public TypeConfigurationCache(ScalarConverterCache scalarConverterCache)
    {
        _scalarConverterCache = scalarConverterCache;
    }

    public IReadOnlyDictionary<Type, TypeConfiguration> Export() => _typeConfigurations;

    public void AddConfiguration(Type type, TypeConfiguration configuration)
    {
        if (_typeConfigurations.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        if (_typesUsingDefaultConfiguration.Contains(type))
        {
            throw new TypeAlreadyRegisteredException(type);
        }

        ValidateEntityBasePropertiesInternal(type, configuration.GetIdPropertyname(), configuration.GetTimestampPropertyName());

        _typeConfigurations.Add(type, configuration);
    }

    public void ValidateEntityBaseProperties(Type type)
    {
        if (!_typeConfigurations.ContainsKey(type) && !_typesUsingDefaultConfiguration.Contains(type))
        {
            ValidateEntityBaseProperties(type);
            _typesUsingDefaultConfiguration.Add(type);
        }
    }

    private void ValidateEntityBasePropertiesInternal(
        Type type,
        string identityPropertyName = Utilities.DefaultIdPropertyName,
        string timestampPropertyName = Utilities.DefaultTimestampPropertyName)
    {
        var properties = type.GetProperties(Utilities.PublicInstance).Where(p => p.GetMethod != null && p.SetMethod != null);

        if (!properties.Any(p => string.Equals(p.Name, identityPropertyName)
            && (Utilities.IdTypes.Contains(p.PropertyType) || Utilities.IdTypes.Any(type => _scalarConverterCache.Contains(p.PropertyType, type)))))
        {
            throw new InvalidEntityBasePropertyException(type, "id", identityPropertyName);
        }

        if (!properties.Any(p => string.Equals(p.Name, timestampPropertyName)
            && (Utilities.TimestampTypes.Contains(p.PropertyType) || Utilities.TimestampTypes.Any(type => _scalarConverterCache.Contains(p.PropertyType, type)))))
        {
            throw new InvalidEntityBasePropertyException(type, "timestamp", timestampPropertyName);
        }
    }
}

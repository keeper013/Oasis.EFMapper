namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal class ExcludedPropertyManager
{
    private readonly Dictionary<Type, ISet<string>> _typeExcludedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, ISet<string>>> _mappingExcludedProperties = new ();

    public bool ContainsTypeConfiguration(Type type)
    {
        return _typeExcludedProperties.ContainsKey(type);
    }

    public (ISet<string>?, ISet<string>?) GetExcludedPropertyNames(Type sourceType, Type targetType)
    {
        var mappingProperties = _mappingExcludedProperties.Find(sourceType, targetType);
        if (mappingProperties == null)
        {
            return (_typeExcludedProperties.TryGetValue(sourceType, out var sourceExcludedProperties) ? sourceExcludedProperties : default,
                _typeExcludedProperties.TryGetValue(targetType, out var targetExcludedProperties) ? targetExcludedProperties : default);
        }
        else
        {
            if (_typeExcludedProperties.TryGetValue(sourceType, out var sourceExcludedProperties))
            {
                sourceExcludedProperties.UnionWith(mappingProperties);
            }
            else
            {
                sourceExcludedProperties = mappingProperties;
            }

            if (_typeExcludedProperties.TryGetValue(targetType, out var targetExcludedProperties))
            {
                targetExcludedProperties.UnionWith(mappingProperties);
            }
            else
            {
                targetExcludedProperties = mappingProperties;
            }

            return (sourceExcludedProperties, targetExcludedProperties);
        }
    }

    public void Add(Type type, ISet<string> excludedProperties)
    {
        if (_typeExcludedProperties.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        _typeExcludedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, ISet<string> excludedProperties)
    {
        if (!_mappingExcludedProperties.TryGetValue(sourceType, out var inner))
        {
            inner = new Dictionary<Type, ISet<string>>();
            _mappingExcludedProperties.Add(sourceType, inner);
        }

        if (inner.ContainsKey(targetType))
        {
            throw new TypeConfiguratedException(targetType);
        }

        inner.Add(targetType, excludedProperties);
    }
}

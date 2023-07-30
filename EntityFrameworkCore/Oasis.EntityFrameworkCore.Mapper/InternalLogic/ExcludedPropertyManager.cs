namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal class ExcludedPropertyManager
{
    private readonly IReadOnlySet<string> _excludedProperties;
    private readonly Dictionary<Type, IReadOnlySet<string>> _typeExcludedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, IReadOnlySet<string>>> _mappingExcludedProperties = new ();

    public ExcludedPropertyManager(IReadOnlySet<string>? excludedProperties)
    {
        _excludedProperties = excludedProperties ?? new HashSet<string>();
    }

    public bool ContainsTypeConfiguration(Type type)
    {
        return _typeExcludedProperties.ContainsKey(type);
    }

    public (ISet<string>?, ISet<string>?) GetExcludedPropertyNames(Type sourceType, Type targetType)
    {
        var mappingProperties = _mappingExcludedProperties.Find(sourceType, targetType);
        ISet<string>? source = default;
        ISet<string>? target = default;
        if (mappingProperties == null)
        {
            if (_typeExcludedProperties.TryGetValue(sourceType, out var sourceExcludedProperties))
            {
                source = new HashSet<string>(sourceExcludedProperties);
                source.UnionWith(_excludedProperties);
            }

            if (_typeExcludedProperties.TryGetValue(targetType, out var targetExcludedProperties))
            {
                target = new HashSet<string>(targetExcludedProperties);
                target.UnionWith(_excludedProperties);
            }
        }
        else
        {
            if (_typeExcludedProperties.TryGetValue(sourceType, out var sourceExcludedProperties))
            {
                source = new HashSet<string>(sourceExcludedProperties);
                source.UnionWith(mappingProperties);
                source.UnionWith(_excludedProperties);
            }
            else
            {
                source = new HashSet<string>(mappingProperties);
                source.UnionWith(_excludedProperties);
            }

            if (_typeExcludedProperties.TryGetValue(targetType, out var targetExcludedProperties))
            {
                target = new HashSet<string>(targetExcludedProperties);
                target.UnionWith(mappingProperties);
                target.UnionWith(_excludedProperties);
            }
            else
            {
                target = new HashSet<string>(mappingProperties);
                target.UnionWith(_excludedProperties);
            }
        }

        return (source, target);
    }

    public void Add(Type type, IReadOnlySet<string> excludedProperties)
    {
        if (_typeExcludedProperties.ContainsKey(type))
        {
            throw new TypeConfiguratedException(type);
        }

        _typeExcludedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, IReadOnlySet<string> excludedProperties)
    {
        if (!_mappingExcludedProperties.TryGetValue(sourceType, out var inner))
        {
            inner = new Dictionary<Type, IReadOnlySet<string>>();
            _mappingExcludedProperties.Add(sourceType, inner);
        }

        if (inner.ContainsKey(targetType))
        {
            throw new TypeConfiguratedException(targetType);
        }

        inner.Add(targetType, excludedProperties);
    }
}

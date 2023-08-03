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
        ISet<string>? source = new HashSet<string>(_excludedProperties);
        ISet<string>? target = new HashSet<string>(_excludedProperties);

        var mappingProperties = _mappingExcludedProperties.Find(sourceType, targetType);
        if (mappingProperties != null)
        {
            source.UnionWith(mappingProperties);
            target.UnionWith(mappingProperties);
        }

        if (_typeExcludedProperties.TryGetValue(sourceType, out var sourceExcludedProperties))
        {
            source.UnionWith(sourceExcludedProperties);
        }

        if (_typeExcludedProperties.TryGetValue(targetType, out var targetExcludedProperties))
        {
            target.UnionWith(targetExcludedProperties);
        }

        return (source, target);
    }

    public void Add(Type type, IReadOnlySet<string> excludedProperties)
    {
        _typeExcludedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, IReadOnlySet<string> excludedProperties)
    {
        if (!_mappingExcludedProperties.TryGetValue(sourceType, out var inner))
        {
            inner = new Dictionary<Type, IReadOnlySet<string>>();
            _mappingExcludedProperties.Add(sourceType, inner);
        }

        inner.Add(targetType, excludedProperties);
    }
}

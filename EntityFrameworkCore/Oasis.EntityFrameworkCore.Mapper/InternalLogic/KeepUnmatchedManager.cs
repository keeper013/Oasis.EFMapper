namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal class KeepUnmatchedManager
{
    private readonly Dictionary<Type, IReadOnlySet<string>> _typeKeepUnmatchedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, IReadOnlySet<string>>> _mappingKeepUnmatchedProperties = new ();

    public bool ContainsTypeConfiguration(Type type)
    {
        return _typeKeepUnmatchedProperties.ContainsKey(type);
    }

    public bool KeepUnmatched(Type sourceType, Type targetType, string propertyName)
    {
        var mappingProperties = _mappingKeepUnmatchedProperties.Find(sourceType, targetType);
        if (mappingProperties != null && mappingProperties.Contains(propertyName))
        {
            return true;
        }

        if (_typeKeepUnmatchedProperties.TryGetValue(targetType, out var keepUnmatchedProperties) && keepUnmatchedProperties.Contains(propertyName))
        {
            return true;
        }

        return false;
    }

    public void Add(Type type, IReadOnlySet<string> excludedProperties)
    {
        _typeKeepUnmatchedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, IReadOnlySet<string> excludedProperties)
    {
        _mappingKeepUnmatchedProperties.AddIfNotExists(sourceType, targetType, excludedProperties);
    }
}

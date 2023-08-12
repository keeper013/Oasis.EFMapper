namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal class KeepUnmatchedManager
{
    private readonly Dictionary<Type, ISet<string>> _typeKeepUnmatchedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, ISet<string>>> _mappingKeepUnmatchedProperties = new ();

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

    public void Add(Type type, ISet<string> excludedProperties)
    {
        _typeKeepUnmatchedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, ISet<string> excludedProperties)
    {
        if (!_mappingKeepUnmatchedProperties.TryGetValue(sourceType, out var inner))
        {
            inner = new Dictionary<Type, ISet<string>>();
            _mappingKeepUnmatchedProperties.Add(sourceType, inner);
        }

        inner.Add(targetType, excludedProperties);
    }
}

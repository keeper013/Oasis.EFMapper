namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal class KeepUnmatchedManager
{
    private readonly Dictionary<Type, ISet<string>> _typeKeepUnmatchedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, ISet<string>>> _mappingKeepUnmatchedProperties = new ();
    private readonly Stack<(Type, Type)> _stack = new ();

    public bool IsEmpty => !_typeKeepUnmatchedProperties.Any() && !_mappingKeepUnmatchedProperties.Any();

    public void Push(Type sourceType, Type targetType)
    {
        _stack.Push((sourceType, targetType));
    }

    public void Pop()
    {
        _stack.Pop();
    }

    public bool ContainsTypeConfiguration(Type type)
    {
        return _typeKeepUnmatchedProperties.ContainsKey(type);
    }

    public bool KeepUnmatched(string propertyName)
    {
        if (_stack.Any())
        {
            var (sourceType, targetType) = _stack.Peek();
            var mappingProperties = _mappingKeepUnmatchedProperties.Find(sourceType, targetType);
            if (mappingProperties != null && mappingProperties.Contains(propertyName))
            {
                return true;
            }

            if (_typeKeepUnmatchedProperties.TryGetValue(targetType, out var keepUnmatchedProperties) && keepUnmatchedProperties.Contains(propertyName))
            {
                return true;
            }
        }

        return false;
    }

    public void Add(Type type, ISet<string> excludedProperties)
    {
        _typeKeepUnmatchedProperties.Add(type, excludedProperties);
    }

    public void Add(Type sourceType, Type targetType, ISet<string> excludedProperties)
    {
        _mappingKeepUnmatchedProperties.AddIfNotExists(sourceType, targetType, excludedProperties);
    }
}

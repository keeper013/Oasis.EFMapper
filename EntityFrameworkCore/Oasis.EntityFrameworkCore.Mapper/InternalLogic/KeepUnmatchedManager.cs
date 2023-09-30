namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal class KeepUnmatchedManager
{
    private readonly Dictionary<Type, IReadOnlySet<string>> _typeKeepUnmatchedProperties = new ();
    private readonly Dictionary<Type, Dictionary<Type, IReadOnlySet<string>>> _mappingKeepUnmatchedProperties = new ();
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

            if (_typeKeepUnmatchedProperties.Contains(targetType, propertyName))
            {
                return true;
            }
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

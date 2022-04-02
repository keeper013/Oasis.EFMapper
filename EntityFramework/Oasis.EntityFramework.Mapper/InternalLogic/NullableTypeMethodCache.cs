namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal sealed class NullableTypeMethodCache
{
    public const string HasValue = "get_HasValue";
    public const string GetValueOrDefault = "GetValueOrDefault";
    public const string Value = "get_Value";
    private readonly Dictionary<Type, Dictionary<string, MethodInfo>> _methodCache = new ();

    public MethodInfo CreateIfNotExist(Type type, string method)
    {
        if (!_methodCache.TryGetValue(type, out var innerDictionary))
        {
            innerDictionary = new Dictionary<string, MethodInfo>();
            _methodCache[type] = innerDictionary;
        }

        if (innerDictionary.TryGetValue(method, out var result))
        {
            return result;
        }

        if (method == GetValueOrDefault)
        {
            result = type.GetMethod(GetValueOrDefault, new Type[0]);
        }
        else
        {
            result = type.GetMethod(method);
        }

        innerDictionary[method] = result!;
        return result!;
    }
}

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;

internal sealed class GenericMethodCache
{
    private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> _dict = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
    private readonly MethodInfo _template;

    public GenericMethodCache(MethodInfo template)
    {
        _template = template;
    }

    public MethodInfo CreateIfNotExist(Type sourceType, Type targetType)
    {
        if (!_dict.TryGetValue(sourceType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, MethodInfo>();
            _dict[sourceType] = innerDictionary;
        }

        if (!innerDictionary.TryGetValue(targetType, out var method))
        {
            method = _template.MakeGenericMethod(sourceType, targetType);
            innerDictionary[targetType] = method;
        }

        return method;
    }
}

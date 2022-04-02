namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal sealed class GenericMapperMethodCache
{
    private readonly IDictionary<Type, IDictionary<Type, MethodInfo>> _dict = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
    private readonly MethodInfo _template;

    public GenericMapperMethodCache(MethodInfo template)
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

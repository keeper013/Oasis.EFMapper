namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal sealed class GenericMapperMethodCache
{
    private readonly Dictionary<Type, Dictionary<Type, MethodInfo>> _dict = new ();
    private readonly MethodInfo _template;

    public GenericMapperMethodCache(MethodInfo template)
    {
        _template = template;
    }

    public MethodInfo CreateIfNotExist(Type sourceType, Type targetType)
    {
        _dict.AddIfNotExists(sourceType, targetType, () => _template.MakeGenericMethod(sourceType, targetType));
        return _dict[sourceType][targetType];
    }
}

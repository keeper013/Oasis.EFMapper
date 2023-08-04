namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal sealed class DependentPropertyManager
{
    private readonly IReadOnlyDictionary<Type, IReadOnlySet<string>> _dependentPropertiesDictionary;

    public DependentPropertyManager(IReadOnlyDictionary<Type, IReadOnlySet<string>> dependentPropertiesDictionary)
    {
        _dependentPropertiesDictionary = dependentPropertiesDictionary;
    }

    public bool IsDependent(Type type, string propertyName)
    {
        return _dependentPropertiesDictionary.TryGetValue(type, out var dependent) && dependent.Contains(propertyName);
    }
}

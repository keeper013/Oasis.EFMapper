namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal sealed class DependentPropertyManager
{
    private readonly IReadOnlyDictionary<Type, ISet<string>> _dependentPropertiesDictionary;

    public DependentPropertyManager(IReadOnlyDictionary<Type, ISet<string>> dependentPropertiesDictionary)
    {
        _dependentPropertiesDictionary = dependentPropertiesDictionary;
    }

    public bool IsDependent(Type type, string propertyName)
    {
        return _dependentPropertiesDictionary.TryGetValue(type, out var dependent) && dependent.Contains(propertyName);
    }
}

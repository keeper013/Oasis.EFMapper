namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal interface IEntityFactory
{
    TEntity Make<TEntity>()
        where TEntity : class;
}

internal class EntityFactory : IEntityFactory
{
    private readonly IReadOnlyDictionary<Type, Delegate> _factoryMethods;

    public EntityFactory(IDictionary<Type, Delegate> factoryMethods, IReadOnlyDictionary<Type, MethodMetaData> generatedConstructors, Type type)
    {
        var dict = new Dictionary<Type, Delegate>(factoryMethods);
        foreach (var kvp in generatedConstructors)
        {
            dict.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name)!));
        }

        _factoryMethods = dict;
    }

    public TEntity Make<TEntity>()
        where TEntity : class
    {
        return ((Func<TEntity>)_factoryMethods[typeof(TEntity)])();
    }
}

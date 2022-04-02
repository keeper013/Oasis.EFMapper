namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal interface IEntityFactory
{
    TEntity Make<TEntity>()
        where TEntity : class;
}

internal class EntityFactory : IEntityFactory
{
    private readonly IReadOnlyDictionary<Type, Delegate> _factoryMethods;

    public EntityFactory(IReadOnlyDictionary<Type, Delegate> factoryMethods)
    {
        _factoryMethods = factoryMethods;
    }

    public TEntity Make<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        return _factoryMethods.TryGetValue(type, out var factoryMethod) ?
            ((Func<TEntity>)factoryMethod)() : (TEntity)Activator.CreateInstance(type)!;
    }
}

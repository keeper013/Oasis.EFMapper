namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;

internal interface IEntityFactory
{
    TEntity Make<TEntity>()
        where TEntity : class;
}

internal class EntityFactory : IEntityFactory
{
    private readonly IReadOnlyDictionary<Type, Delegate> _factoryMethods;
    private readonly Dictionary<Type, Delegate> _generatedConstructors = new ();

    public EntityFactory(IReadOnlyDictionary<Type, Delegate> factoryMethods, IReadOnlyDictionary<Type, MethodMetaData> generatedConstructors, Type type)
    {
        _factoryMethods = factoryMethods;
        foreach (var kvp in generatedConstructors)
        {
            _generatedConstructors.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name)!));
        }
    }

    public TEntity Make<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_factoryMethods.TryGetValue(type, out var factoryMethod))
        {
            return ((Func<TEntity>)factoryMethod)();
        }
        else if (_generatedConstructors.TryGetValue(type, out var generatedConstructor))
        {
            return ((Func<TEntity>)generatedConstructor)();
        }

        throw new InvalidOperationException($"Type {type} doesn't have custom either factory method or a generated construct method.");
    }
}

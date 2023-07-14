namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal interface IEntityFactory
{
    TEntity Make<TEntity>()
        where TEntity : class;
}

internal class EntityFactory : IEntityFactory
{
    private readonly IDictionary<Type, Delegate> _factoryMethods;

    public EntityFactory(IDictionary<Type, Delegate> factoryMethods)
    {
        _factoryMethods = factoryMethods;
    }

    public TEntity Make<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (_factoryMethods.TryGetValue(type, out var factoryMethod))
        {
            return ((Func<TEntity>)factoryMethod)();
        }
        else
        {
            var constructorInfo = type.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>());

            if (constructorInfo != null)
            {
                Func<TEntity> func = () => (TEntity)constructorInfo.Invoke(Array.Empty<object>());
                _factoryMethods.Add(type, func);
                return func();
            }
        }

        throw new UnconstructableTypeException(type);
    }
}

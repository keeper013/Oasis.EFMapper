namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface IEntityBaseProxy
{
    object GetId<TEntity>(TEntity entity)
        where TEntity : class;

    bool IdIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    bool TimeStampIsEmpty<TEntity>(TEntity entity)
        where TEntity : class;

    bool IdEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
        where TEntity1 : class
        where TEntity2 : class;

    bool TimeStampEquals<TEntity1, TEntity2>(TEntity1 entity1, TEntity2 entity2)
        where TEntity1 : class
        where TEntity2 : class;
}

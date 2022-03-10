namespace Oasis.EntityFrameworkCore.Mapper;

public interface IListPropertyMapper
{
    void MapListProperty<TSource, TTarget>(
        ICollection<TSource> source, ICollection<TTarget> target)
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase, new();
}

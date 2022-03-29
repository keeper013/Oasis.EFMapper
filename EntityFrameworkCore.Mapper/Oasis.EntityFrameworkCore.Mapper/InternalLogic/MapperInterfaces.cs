namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public interface IScalarTypeConverter
{
    TTarget Convert<TSource, TTarget>(TSource source);

    object? Convert(object? value, Type targetType);
}

public interface IListPropertyMapper
{
    void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target)
        where TSource : class
        where TTarget : class;

    TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}

public interface IEntityPropertyMapper
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class
        where TTarget : class;
}

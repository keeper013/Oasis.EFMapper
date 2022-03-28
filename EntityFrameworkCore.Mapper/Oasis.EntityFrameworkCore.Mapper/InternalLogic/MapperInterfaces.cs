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
}

public interface IEntityPropertyMapper
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target)
        where TSource : class
        where TTarget : class;
}

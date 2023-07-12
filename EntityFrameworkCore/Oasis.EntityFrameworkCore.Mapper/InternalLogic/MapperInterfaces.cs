namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public interface IScalarTypeConverter
{
    TTarget Convert<TSource, TTarget>(TSource source);

    object? Convert(object? value, Type targetType);
}

public interface INewTargetTracker<TKeyType>
    where TKeyType : struct
{
    bool NewTargetIfNotExist<TTarget>(TKeyType key, out TTarget target)
        where TTarget : class;
}

public interface IListPropertyMapper<TKeyType>
    where TKeyType : struct
{
    void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<TKeyType> newTargetTracker)
        where TSource : class
        where TTarget : class;

    TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}

public interface IEntityPropertyMapper<TKeyType>
    where TKeyType : struct
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<TKeyType> newTargetTracker)
        where TSource : class
        where TTarget : class;
}

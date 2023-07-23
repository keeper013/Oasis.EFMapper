namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public interface IScalarTypeConverter
{
    TTarget Convert<TSource, TTarget>(TSource source);

    object? Convert(object? value, Type targetType);
}

/// <summary>
/// This interface is used to avoid infinite loop when mapping (e.g. A -> B -> C -> A).
/// When such infinite loop is detected (mapping an entity that has been mapped before, the process will not map the entity again).
/// </summary>
/// <typeparam name="TKeyType">Type of identifyer of new instance in memory, normally should be type of hash code.</typeparam>
public interface INewTargetTracker<TKeyType>
    where TKeyType : struct
{
    /// <summary>
    /// Create a new instance of mapping target if the source hasn't been mapped.
    /// </summary>
    /// <typeparam name="TTarget">Type of mapping target.</typeparam>
    /// <param name="key">Key of source, usually hash code of the instance.</param>
    /// <param name="target">Instance of target.</param>
    /// <returns>true if source of the key has been mapped, else false.</returns>
    bool NewTargetIfNotExist<TTarget>(TKeyType key, out TTarget target)
        where TTarget : class;
}

public record struct EntityPropertyMappingData(Type sourceType, Type targetType, string propertyName);

/// <summary>
/// Recursive mapper interface. This interface has to be public, or else generate code will have problem accessing its methods.
/// </summary>
/// <typeparam name="TKeyType">Type of target tracker key, int if using hash code.</typeparam>
public interface IRecursiveMapper<TKeyType>
    where TKeyType : struct
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, INewTargetTracker<TKeyType>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class;

    void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, INewTargetTracker<TKeyType>? newTargetTracker, string propertyName)
        where TSource : class
        where TTarget : class;

    TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}
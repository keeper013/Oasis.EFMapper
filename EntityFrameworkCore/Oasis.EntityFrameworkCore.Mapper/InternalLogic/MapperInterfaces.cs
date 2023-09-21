namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public interface IScalarTypeConverter
{
    TTarget Convert<TSource, TTarget>(TSource source);
}

public interface IEntityTracker<TTarget>
    where TTarget : class
{
    void Track(TTarget target);
}

public interface IRecursiveMappingContext
{
    bool NeedToTrackEntity<TSource, TTarget>()
            where TSource : class
            where TTarget : class;

    /// <summary>
    /// If target is tracked by hash code or id, return tracked target, or else make a new target and track it, then return the target.
    /// </summary>
    /// <typeparam name="TSource">Source type.</typeparam>
    /// <typeparam name="TTarget">Target type.</typeparam>
    /// <param name="source">Source object.</param>
    /// <param name="tracker">Entity tracker in case target is not traced.</param>
    /// <returns>true if the target has not tracked before calling the function else false.</returns>
    TTarget? GetTracked<TSource, TTarget>(TSource source, out IEntityTracker<TTarget>? tracker)
        where TSource : class
        where TTarget : class;

    IEntityTracker<TTarget> GetTracker<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;

    void Clear();
}

/// <summary>
/// Recursive mapper interface. This interface has to be public, or else generate code will have problem accessing its methods.
/// </summary>
/// <typeparam name="TKeyType">Type of target tracker key, int if using hash code.</typeparam>
public interface IRecursiveMapper<TKeyType>
    where TKeyType : struct
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class;

    void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, IRecursiveMappingContext context, string propertyName)
        where TSource : class
        where TTarget : class;

    TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}
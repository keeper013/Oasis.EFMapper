namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;

public interface IRecursiveMappingContext
{
    bool ForceTrack { get; set; }

    DbContext? DatabaseContext { get; set; }

    bool HasTracked { get; }

    void Clear();

    TTarget? GetTracked<TSource, TTarget>(TSource source, out IEntityTracker<TTarget>? tracker)
        where TSource : class
        where TTarget : class;

    IEntityTracker<TTarget> GetTracker<TSource, TTarget>(TSource source)
        where TSource : class
        where TTarget : class;
}

public interface IEntityTracker<TTarget>
    where TTarget : class
{
    void Track(TTarget target);
}

/// <summary>
/// Recursive mapper interface. This interface has to be public, or else generate code will have problem accessing its methods.
/// </summary>
/// <typeparam name="TKeyType">Type of target tracker key, int if using hash code.</typeparam>
public interface IRecursiveMapper<TKeyType>
    where TKeyType : struct
{
    TTarget? MapEntityProperty<TSource, TTarget>(TSource? source, TTarget? target, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class;

    void MapListProperty<TSource, TTarget>(ICollection<TSource>? source, ICollection<TTarget> target, string propertyName, IRecursiveMappingContext context)
        where TSource : class
        where TTarget : class;

    TList ConstructListType<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}
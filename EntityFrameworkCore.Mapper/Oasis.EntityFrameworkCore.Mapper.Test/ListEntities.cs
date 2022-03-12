namespace Oasis.EntityFrameworkCore.Mapper.Test;

using System.Collections.Generic;

public sealed class CollectionEntity1 : EntityBase
{
    public CollectionEntity1()
    {
    }

    public CollectionEntity1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionEntity1(long? id, int intProp, ICollection<ScalarEntity1> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarEntity1>? Scs { get; set; }
}

public sealed class CollectionEntity2 : EntityBase
{
    public CollectionEntity2()
    {
    }

    public CollectionEntity2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionEntity2(long? id, int intProp, ICollection<ScalarEntity2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarEntity2>? Scs { get; set; }
}

public sealed class ListIEntity1 : EntityBase
{
    public ListIEntity1()
    {
    }

    public ListIEntity1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ListIEntity1(long? id, int intProp, IList<ScalarEntity2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public IList<ScalarEntity2>? Scs { get; set; }
}

public sealed class ListEntity1 : EntityBase
{
    public ListEntity1()
    {
    }

    public ListEntity1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ListEntity1(long? id, int intProp, List<ScalarEntity2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public List<ScalarEntity2>? Scs { get; set; }
}

public sealed class RecursiveEntity1 : EntityBase
{
    public RecursiveEntity1()
    {
    }

    public RecursiveEntity1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public RecursiveEntity1(long? id, int intProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public IList<RecursiveEntity1>? SubItems { get; set; }
}

public sealed class RecursiveEntity2 : EntityBase
{
    public RecursiveEntity2()
    {
    }

    public RecursiveEntity2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public RecursiveEntity2(long? id, int intProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public IList<RecursiveEntity2>? SubItems { get; set; }
}
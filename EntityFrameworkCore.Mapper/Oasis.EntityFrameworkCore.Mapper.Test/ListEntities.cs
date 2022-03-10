namespace Oasis.EntityFrameworkCore.Mapper.Test;

using System.Collections.Generic;

public sealed class CollectionClass1 : EntityBase
{
    public CollectionClass1()
    {
    }

    public CollectionClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionClass1(long? id, int intProp, ICollection<ScalarClass1> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarClass1>? Scs { get; set; }
}

public sealed class CollectionClass2 : EntityBase
{
    public CollectionClass2()
    {
    }

    public CollectionClass2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public CollectionClass2(long? id, int intProp, ICollection<ScalarClass2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarClass2>? Scs { get; set; }
}

public sealed class ListIClass1 : EntityBase
{
    public ListIClass1()
    {
    }

    public ListIClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ListIClass1(long? id, int intProp, IList<ScalarClass2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public IList<ScalarClass2>? Scs { get; set; }
}

public sealed class ListClass1 : EntityBase
{
    public ListClass1()
    {
    }

    public ListClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ListClass1(long? id, int intProp, List<ScalarClass2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public List<ScalarClass2>? Scs { get; set; }
}
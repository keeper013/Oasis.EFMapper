namespace Oasis.EntityFrameworkCore.Mapper.Test;

using System.Collections.Generic;

public sealed class CollectionEntity1 : EntityBase
{
    public CollectionEntity1()
    {
    }

    public CollectionEntity1(int intProp, ICollection<SubScalarEntity1> scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<SubScalarEntity1>? Scs { get; set; }
}

public sealed class CollectionEntity2 : EntityBase
{
    public CollectionEntity2()
    {
    }

    public CollectionEntity2(int intProp, ICollection<ScalarEntity2> scs)
    {
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

    public ListIEntity1(int intProp, IList<SubScalarEntity1> scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public IList<SubScalarEntity1>? Scs { get; set; }
}

public sealed class ListEntity1 : EntityBase
{
    public ListEntity1()
    {
    }

    public ListEntity1(int intProp, List<SubScalarEntity1> scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public List<SubScalarEntity1>? Scs { get; set; }
}

public sealed class SubScalarEntity1 : EntityBase
{
    public SubScalarEntity1()
    {
    }

    public SubScalarEntity1(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public long? CollectionEntityId { get; set; }

    public long? ListIEntityId { get; set; }

    public long? ListEntityId { get; set; }

    public int IntProp { get; set; }

    public long? LongNullableProp { get; set; }

    public string? StringProp { get; set; }

    public byte[]? ByteArrayProp { get; set; }

    public CollectionEntity1? CollectionEntity { get; set; }

    public ListIEntity1? ListIEntity { get; set; }

    public ListEntity1? ListEntity { get; set; }
}

public sealed class RecursiveEntity1 : EntityBase
{
    public RecursiveEntity1()
    {
    }

    public RecursiveEntity1(int intProp)
    {
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

    public RecursiveEntity2(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public IList<RecursiveEntity2>? SubItems { get; set; }
}
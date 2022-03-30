namespace Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;

using Oasis.EntityFrameworkCore.Mapper.Test.Scalar;
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

public sealed class ListEntity2 : EntityBase
{
    public ListEntity2()
    {
    }

    public ListEntity2(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public List<SubEntity2>? SubEntities { get; set; }
}

public sealed class SubEntity2 : EntityBase
{
    public SubEntity2()
    {
    }

    public SubEntity2(string? stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }

    public long? ListEntityId { get; set; }

    public ListEntity2? ListEntity { get; set; }
}

public sealed class ListEntity3 : EntityBase
{
    public ListEntity3()
    {
    }

    public ListEntity3(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public List<SubEntity3>? SubEntities { get; set; }
}

public sealed class SubEntity3 : EntityBase
{
    public SubEntity3()
    {
    }

    public SubEntity3(string? stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }

    public long? ListEntityId { get; set; }
}

public sealed class CollectionEntity2WithWrapper : EntityBase
{
    public CollectionEntity2WithWrapper()
    {
    }

    public CollectionEntity2WithWrapper(int intProp, ScalarEntity2ListWrapper scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ScalarEntity2ListWrapper? Scs { get; set; }
}

public sealed class CollectionEntity4WithWrapper : EntityBase
{
    public CollectionEntity4WithWrapper()
    {
    }

    public CollectionEntity4WithWrapper(int intProp, ScalarEntity2ListWrapper scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ScalarEntity2ListWrapper? Scs { get; }
}

public sealed class CollectionEntity3WithWrapper : EntityBase
{
    public CollectionEntity3WithWrapper()
    {
    }

    public CollectionEntity3WithWrapper(int intProp, ScalarEntity2NoDefaultConstructorListWrapper scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ScalarEntity2NoDefaultConstructorListWrapper? Scs { get; set; }
}

public sealed class ScalarEntity2ListWrapper : List<ScalarEntity2>
{
}

public sealed class ScalarEntity2NoDefaultConstructorListWrapper : List<ScalarEntity2>
{
    public ScalarEntity2NoDefaultConstructorListWrapper(int count)
    {
    }
}

public sealed class StringListNoDefaultConstructor : List<string>
{
    public StringListNoDefaultConstructor(int count)
    {
    }
}
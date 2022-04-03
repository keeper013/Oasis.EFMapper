namespace Oasis.EntityFramework.Mapper.Test.OneToMany;

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

public sealed class ScalarEntity1Item : EntityBase
{
    public ScalarEntity1Item()
    {
    }

    public ScalarEntity1Item(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int IntProp { get; set; }

    public long? LongNullableProp { get; set; }

    public string? StringProp { get; set; }

    public byte[]? ByteArrayProp { get; set; }

    public long DerivedEntity1Id { get; set; }

    public long DerivedEntity1_1Id { get; set; }

    public DerivedEntity1? DerivedEntity1 { get; set; }

    public DerivedEntity1_1? DerivedEntity1_1 { get; set; }
}

public sealed class ScalarEntity2Item : EntityBase
{
    public ScalarEntity2Item()
    {
    }

    public ScalarEntity2Item(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int IntProp { get; set; }

    public long? LongNullableProp { get; set; }

    public string? StringProp { get; set; }

    public byte[]? ByteArrayProp { get; set; }
}

public sealed class ScalarItem1 : EntityBase
{
    public string? StringProp { get; set; }

    public long? List1Id { get; set; }

    public long? List2Id { get; set; }

    public SessionTestingList1_1? List1 { get; set; }

    public SessionTestingList1_2? List2 { get; set; }
}

public sealed class ScalarItem2 : EntityBase
{
    public ScalarItem2(string prop)
    {
        StringProp = prop;
    }

    public string? StringProp { get; set; }
}
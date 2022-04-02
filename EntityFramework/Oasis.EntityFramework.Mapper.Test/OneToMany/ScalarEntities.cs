namespace Oasis.EntityFramework.Mapper.Test.OneToMany;

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

namespace Oasis.EntityFramework.Mapper.Test.ToDatabase;

public sealed class ToDatabaseEntity1 : NullableIdEntityBase
{
    public ToDatabaseEntity1()
    {
    }

    public ToDatabaseEntity1(long? id, long? concurrencyToken, int prop)
    {
        Id = id;
        ConcurrencyToken = concurrencyToken;
        IntProperty = prop;
    }

    public int IntProperty { get; set; }
}

public sealed class ToDatabaseEntity2 : NullableIdEntityBase
{
    public ToDatabaseEntity2(long? id, long? concurrencyToken, int prop)
    {
        Id = id;
        ConcurrencyToken = concurrencyToken;
        IntProperty = prop;
    }

    public int IntProperty { get; set; }
}
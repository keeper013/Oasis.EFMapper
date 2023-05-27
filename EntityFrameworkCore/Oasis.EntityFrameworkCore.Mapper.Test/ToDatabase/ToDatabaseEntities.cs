namespace Oasis.EntityFrameworkCore.Mapper.Test.ToDatabase;

public sealed class ToDatabaseEntity1 : NullableIdEntityBase
{
    public ToDatabaseEntity1()
    {
    }

    public ToDatabaseEntity1(long? id, byte[]? timestamp, int prop)
    {
        Id = id;
        Timestamp = timestamp;
        IntProperty = prop;
    }

    public int IntProperty { get; set; }
}

public sealed class ToDatabaseEntity2 : NullableIdEntityBase
{
    public ToDatabaseEntity2(long? id, byte[]? timestamp, int prop)
    {
        Id = id;
        Timestamp = timestamp;
        IntProperty = prop;
    }

    public int IntProperty { get; set; }
}
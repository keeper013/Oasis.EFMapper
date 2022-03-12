namespace Oasis.EntityFrameworkCore.Mapper.Test;

public sealed class ScalarEntity1 : EntityBase
{
    public ScalarEntity1()
    {
    }

    public ScalarEntity1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarEntity1(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
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

public sealed class ScalarEntity2 : EntityBase
{
    public ScalarEntity2()
    {
    }

    public ScalarEntity2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarEntity2(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
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

public sealed class ScalarEntity3 : EntityBase
{
    public ScalarEntity3()
    {
    }

    public ScalarEntity3(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarEntity3(long? id, int? intProp, long longNullableProp, string stringProp, char[] byteArrayProp)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        LongNullableProp = longNullableProp;
        StringProp1 = stringProp;
        ByteArrayProp = byteArrayProp;
    }

    public int? IntProp { get; set; }

    public long LongNullableProp { get; set; }

    public string? StringProp1 { get; set; }

    public char[]? ByteArrayProp { get; set; }
}

public class ScalarEntity4 : EntityBase
{
    public ScalarEntity4()
    {
    }

    public ScalarEntity4(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarEntity4(long? id, byte[] content)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        ByteArrayProp = new ByteArrayWrapper(content);
    }

    public ByteArrayWrapper? ByteArrayProp { get; set; }
}

public sealed class ByteArrayWrapper
{
    private readonly byte[] _bytes;

    public ByteArrayWrapper(byte[] content)
    {
        _bytes = content;
    }

    public byte[] Bytes => _bytes;

    public static byte[] ConvertStatic(ByteArrayWrapper wrapper)
    {
        return wrapper.Bytes;
    }

    public static ByteArrayWrapper ConvertStatic(byte[] array)
    {
        return new ByteArrayWrapper(array);
    }

    public byte[] ConvertInstance(ByteArrayWrapper wrapper)
    {
        return wrapper.Bytes;
    }
}

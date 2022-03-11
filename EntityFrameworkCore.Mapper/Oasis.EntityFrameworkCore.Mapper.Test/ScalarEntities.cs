namespace Oasis.EntityFrameworkCore.Mapper.Test;

public sealed class ScalarClass1 : EntityBase
{
    public ScalarClass1()
    {
    }

    public ScalarClass1(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass1(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarClass2 : EntityBase
{
    public ScalarClass2()
    {
    }

    public ScalarClass2(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass2(long? id, int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarClass3 : EntityBase
{
    public ScalarClass3()
    {
    }

    public ScalarClass3(long? id)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
    }

    public ScalarClass3(long? id, int? intProp, long longNullableProp, string stringProp, char[] byteArrayProp)
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

public class ScalarClass4 : EntityBase
{
    public ScalarClass4()
    {
    }

    public ScalarClass4(byte[] content)
    {
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

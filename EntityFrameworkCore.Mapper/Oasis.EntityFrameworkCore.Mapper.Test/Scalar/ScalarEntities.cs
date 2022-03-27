﻿namespace Oasis.EntityFrameworkCore.Mapper.Test.Scalar;

public sealed class ScalarEntity1 : EntityBase
{
    public ScalarEntity1()
    {
    }

    public ScalarEntity1(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarEntity2 : EntityBase
{
    public ScalarEntity2()
    {
    }

    public ScalarEntity2(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarEntity3 : EntityBase
{
    public ScalarEntity3()
    {
    }

    public ScalarEntity3(int? intProp, long longNullableProp, string stringProp, char[] byteArrayProp)
    {
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

    public ScalarEntity4(byte[] content)
    {
        ByteArrayProp = new ByteArrayWrapper(content);
    }

    public ByteArrayWrapper? ByteArrayProp { get; set; }
}

public sealed class ScalarEntityNoBase1
{
    public ScalarEntityNoBase1()
    {
    }

    public ScalarEntityNoBase1(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarEntityNoBase2
{
    public ScalarEntityNoBase2()
    {
    }

    public ScalarEntityNoBase2(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarEntityCustomKeyProperties1 : ReversedEntityBase
{
    public ScalarEntityCustomKeyProperties1()
    {
    }

    public ScalarEntityCustomKeyProperties1(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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

public sealed class ScalarEntityNoTimeStamp1 : EntityBaseNoTimeStamp
{
    public ScalarEntityNoTimeStamp1()
    {
    }

    public ScalarEntityNoTimeStamp1(int intProp, long? longNullableProp, string stringProp, byte[] byteArrayProp)
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
}

public sealed class EntityWithoutDefaultConstructor : EntityBase
{
    public EntityWithoutDefaultConstructor(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }
}

namespace Oasis.EntityFramework.Mapper.Test.KeyPropertyType;

using System;

public abstract class SomeSourceEntity<T> : EntityBase<T>
{
    public SomeSourceEntity()
    {
    }

    public SomeSourceEntity(int prop)
    {
        SomeProperty = prop;
    }

    public int SomeProperty { get; set; }
}

public sealed class SomeTargetEntity<T> : EntityBase<T>
{
    public SomeTargetEntity()
    {
    }

    public SomeTargetEntity(int prop)
    {
        SomeProperty = prop;
    }

    public int SomeProperty { get; set; }
}

public sealed class ByteSourceEntity : SomeSourceEntity<byte>
{
    public ByteSourceEntity()
    {
    }

    public ByteSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class NByteSourceEntity : SomeSourceEntity<byte?>
{
    public NByteSourceEntity()
    {
    }

    public NByteSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class ShortSourceEntity : SomeSourceEntity<short>
{
    public ShortSourceEntity()
    {
    }

    public ShortSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class NShortSourceEntity : SomeSourceEntity<short?>
{
    public NShortSourceEntity()
    {
    }

    public NShortSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class IntSourceEntity : SomeSourceEntity<int>
{
    public IntSourceEntity()
    {
    }

    public IntSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class NIntSourceEntity : SomeSourceEntity<int?>
{
    public NIntSourceEntity()
    {
    }

    public NIntSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class LongSourceEntity : SomeSourceEntity<long>
{
    public LongSourceEntity()
    {
    }

    public LongSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
public sealed class NLongSourceEntity : SomeSourceEntity<long?>
{
    public NLongSourceEntity()
    {
    }

    public NLongSourceEntity(int intProp)
        : base(intProp)
    {
    }
}
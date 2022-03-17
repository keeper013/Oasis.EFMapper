namespace Oasis.EntityFrameworkCore.Mapper.Test;

public sealed class Outer1 : EntityBase
{
    public Outer1()
    {
    }

    public Outer1(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public long? InnerId { get; set; }

    public Inner1? Inner { get; set; }
}

public class Inner1 : EntityBase
{
    public Inner1()
    {
    }

    public Inner1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }

    public long? OuterId { get; set; }

    public Outer1? Outer { get; set; }
}

public sealed class Outer2 : EntityBase
{
    public Outer2()
    {
    }

    public Outer2(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public long? InnerId { get; set; }

    public Inner2? Inner { get; set; }
}

public class Inner2 : EntityBase
{
    public Inner2()
    {
    }

    public Inner2(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }

    public long? OuterId { get; set; }

    public Outer2? Outer { get; set; }
}
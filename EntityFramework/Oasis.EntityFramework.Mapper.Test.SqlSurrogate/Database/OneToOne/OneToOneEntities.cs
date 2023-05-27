namespace Oasis.EntityFramework.Mapper.Test.OneToOne;

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

    public Inner1_1? Inner1 { get; set; }

    public Inner1_2? Inner2 { get; set; }
}

public class Inner1_1 : EntityBase
{
    public Inner1_1()
    {
    }

    public Inner1_1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }

    public long? OuterId { get; set; }

    public Outer1? Outer { get; set; }
}

public class Inner1_2 : EntityBase
{
    public Inner1_2()
    {
    }

    public Inner1_2(string stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }

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

    public Inner2_1? Inner1 { get; set; }

    public Inner2_2? Inner2 { get; set; }
}

public class Inner2_1 : EntityBase
{
    public Inner2_1()
    {
    }

    public Inner2_1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }
}

public class Inner2_2 : EntityBase
{
    public Inner2_2()
    {
    }

    public Inner2_2(string stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }
}

public class RecursiveEntity1 : EntityBase
{
    public RecursiveEntity1()
    {
    }

    public RecursiveEntity1(string stringProperty)
    {
        StringProperty = stringProperty;
    }

    public string? StringProperty { get; set; }

    public long? ParentId { get; set; }

    public RecursiveEntity1? Parent { get; set; }

    public RecursiveEntity1? Child { get; set; }
}

public class RecursiveEntity2 : EntityBase
{
    public RecursiveEntity2()
    {
    }

    public RecursiveEntity2(string stringProperty)
    {
        StringProperty = stringProperty;
    }

    public string? StringProperty { get; set; }

    public long? ParentId { get; set; }

    public RecursiveEntity2? Parent { get; set; }

    public RecursiveEntity2? Child { get; set; }
}
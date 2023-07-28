namespace Oasis.EntityFramework.Mapper.Test.OneToOne;

public sealed class PrincipalOptional1 : EntityBase
{
    public PrincipalOptional1()
    {
    }

    public PrincipalOptional1(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public DependentOptional1_1? Inner1 { get; set; }

    public DependentOptional1_2? Inner2 { get; set; }
}

public class DependentOptional1_1 : EntityBase
{
    public DependentOptional1_1()
    {
    }

    public DependentOptional1_1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }

    public PrincipalOptional1? Outer { get; set; }
}

public class DependentOptional1_2 : EntityBase
{
    public DependentOptional1_2()
    {
    }

    public DependentOptional1_2(string stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }

    public PrincipalOptional1? Outer { get; set; }
}

public sealed class PrincipalOptional2 : EntityBase
{
    public PrincipalOptional2()
    {
    }

    public PrincipalOptional2(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public Dependent2_1? Inner1 { get; set; }

    public Dependent2_2? Inner2 { get; set; }
}

public sealed class PrincipalRequired1 : EntityBase
{
    public PrincipalRequired1()
    {
    }

    public PrincipalRequired1(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public DependentRequired1_1 Inner1 { get; set; } = null!;

    public DependentRequired1_2 Inner2 { get; set; } = null!;
}

public class DependentRequired1_1 : EntityBase
{
    public DependentRequired1_1()
    {
    }

    public DependentRequired1_1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }

    public PrincipalRequired1 Outer { get; set; } = null!;
}

public class DependentRequired1_2 : EntityBase
{
    public DependentRequired1_2()
    {
    }

    public DependentRequired1_2(string stringProp)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }

    public PrincipalRequired1 Outer { get; set; } = null!;
}

public sealed class PrincipalRequired2 : EntityBase
{
    public PrincipalRequired2()
    {
    }

    public PrincipalRequired2(int intProp)
    {
        IntProp = intProp;
    }

    public int IntProp { get; set; }

    public Dependent2_1 Inner1 { get; set; } = null!;

    public Dependent2_2 Inner2 { get; set; } = null!;
}

public class Dependent2_1 : EntityBase
{
    public Dependent2_1()
    {
    }

    public Dependent2_1(long longProp)
    {
        LongProp = longProp;
    }

    public long LongProp { get; set; }
}

public class Dependent2_2 : EntityBase
{
    public Dependent2_2()
    {
    }

    public Dependent2_2(string stringProp)
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

    public RecursiveEntity2? Parent { get; set; }

    public RecursiveEntity2? Child { get; set; }
}
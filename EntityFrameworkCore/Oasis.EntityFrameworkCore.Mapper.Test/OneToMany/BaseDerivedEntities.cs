namespace Oasis.EntityFrameworkCore.Mapper.Test.OneToMany;

using System.Collections.Generic;

public class BaseEntity1 : EntityBase
{
    public BaseEntity1()
    {
    }

    public BaseEntity1(int intProp, List<ScalarEntity1Item> scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public List<ScalarEntity1Item>? Scs { get; set; }
}

public class BaseEntity2 : EntityBase
{
    public BaseEntity2()
    {
    }

    public BaseEntity2(int intProp, ICollection<ScalarEntity2Item> scs)
    {
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarEntity2Item>? Scs { get; set; }
}

public class DerivedEntity1 : BaseEntity1
{
    public DerivedEntity1()
    {
    }

    public DerivedEntity1(string stringProp, int intProp, List<ScalarEntity1Item> scs)
        : base(intProp, scs)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }
}

public class DerivedEntity2 : BaseEntity2
{
    public DerivedEntity2()
    {
    }

    public DerivedEntity2(string stringProp, int intProp, ICollection<ScalarEntity2Item> scs)
        : base(intProp, scs)
    {
        StringProp = stringProp;
    }

    public string? StringProp { get; set; }
}

public class DerivedEntity1_1 : BaseEntity1
{
    public DerivedEntity1_1()
    {
    }

    public DerivedEntity1_1(int newIntProp, int intProp, List<ScalarEntity1Item> scs)
        : base(intProp, scs)
    {
        IntProp = newIntProp;
    }

    public new int IntProp { get; set; }
}

public class DerivedEntity2_2 : BaseEntity2
{
    public DerivedEntity2_2()
    {
    }

    public DerivedEntity2_2(int newIntProp, int intProp, ICollection<ScalarEntity2Item> scs)
        : base(intProp, scs)
    {
        IntProp = newIntProp;
    }

    public new int IntProp { get; set; }
}
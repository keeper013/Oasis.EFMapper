namespace Oasis.EntityFrameworkCore.Mapper.Test;

using System.Collections.Generic;

public class BaseEntity1 : EntityBase
{
    public BaseEntity1()
    {
    }

    public BaseEntity1(long? id, int intProp, List<ScalarEntity1> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public List<ScalarEntity1>? Scs { get; set; }
}

public class BaseEntity2 : EntityBase
{
    public BaseEntity2()
    {
    }

    public BaseEntity2(long? id, int intProp, ICollection<ScalarEntity2> scs)
    {
        Id = id;
        Timestamp = id.HasValue ? DatabaseContext.DefaultTimeStamp : null;
        IntProp = intProp;
        Scs = scs;
    }

    public int IntProp { get; set; }

    public ICollection<ScalarEntity2>? Scs { get; set; }
}

public class DerivedEntity1 : BaseEntity1
{
    public DerivedEntity1()
    {
    }

    public DerivedEntity1(long? id, string stringProp, int intProp, List<ScalarEntity1> scs)
        : base(id, intProp, scs)
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

    public DerivedEntity2(long? id, string stringProp, int intProp, ICollection<ScalarEntity2> scs)
        : base(id, intProp, scs)
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

    public DerivedEntity1_1(long? id, int newIntProp, int intProp, List<ScalarEntity1> scs)
        : base(id, intProp, scs)
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

    public DerivedEntity2_2(long? id, int newIntProp, int intProp, ICollection<ScalarEntity2> scs)
        : base(id, intProp, scs)
    {
        IntProp = newIntProp;
    }

    public new int IntProp { get; set; }
}
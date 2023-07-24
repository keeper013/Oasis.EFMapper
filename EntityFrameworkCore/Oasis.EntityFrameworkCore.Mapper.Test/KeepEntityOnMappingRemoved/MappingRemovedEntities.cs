namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepEntityOnMappingRemoved;

using System.Collections.Generic;

public class Principal1 : EntityBase
{
    public Dependant1? OptionalDependant { get; set; } = null!;

    public IList<Dependant1> DependentList { get; set; } = null!;
}

public class Dependant1 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

public class Principal2 : EntityBase
{
    public Dependant2? OptionalDependant { get; set; } = null!;

    public IList<Dependant2> DependentList { get; set; } = null!;
}

public class Dependant2 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}
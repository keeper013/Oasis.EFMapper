namespace Oasis.EntityFrameworkCore.Mapper.Test.DependentProperty;

using System.Collections.Generic;

public sealed class DependentPropertyPrincipal1 : EntityBase
{
    public DependentPropertyDependent1? OptionalDependent { get; set; } = null!;

    public IList<DependentPropertyDependent1> DependentList { get; set; } = null!;
}

public sealed class DependentPropertyDependent1 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

public sealed class DependentPropertyPrincipal2 : EntityBase
{
    public DependentPropertyDependent2? OptionalDependent { get; set; } = null!;

    public IList<DependentPropertyDependent2> DependentList { get; set; } = null!;
}

public sealed class DependentPropertyDependent2 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}
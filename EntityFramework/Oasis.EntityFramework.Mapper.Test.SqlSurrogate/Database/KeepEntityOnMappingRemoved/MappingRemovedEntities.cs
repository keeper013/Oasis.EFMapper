namespace Oasis.EntityFramework.Mapper.Test.KeepEntityOnMappingRemoved;

using System.Collections.Generic;

public sealed class MappingRemovedPrincipal1 : EntityBase
{
    public MappingRemovedDependant1? OptionalDependant { get; set; } = null!;

    public IList<MappingRemovedDependant1> DependantList { get; set; } = null!;
}

public sealed class MappingRemovedDependant1 : EntityBase
{
    public long? PrincipalIdForEntity { get; set; }

    public long? PrincipalIdForList { get; set; }

    public int IntProp { get; set; }

    public MappingRemovedPrincipal1? PrincipalForEntity { get; set; }

    public MappingRemovedPrincipal1? PrincipalForList { get; set; }
}

public sealed class MappingRemovedPrincipal2 : EntityBase
{
    public MappingRemovedDependant2? OptionalDependant { get; set; } = null!;

    public IList<MappingRemovedDependant2> DependantList { get; set; } = null!;
}

public sealed class MappingRemovedDependant2 : EntityBase
{
    public long? PrincipalIdForEntity { get; set; }

    public long? PrincipalIdForList { get; set; }

    public int IntProp { get; set; }

    public MappingRemovedPrincipal2? PrincipalForEntity { get; set; }

    public MappingRemovedPrincipal2? PrincipalForList { get; set; }
}
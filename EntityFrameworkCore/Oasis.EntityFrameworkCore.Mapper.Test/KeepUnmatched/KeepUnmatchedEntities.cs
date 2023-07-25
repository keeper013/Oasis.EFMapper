namespace Oasis.EntityFrameworkCore.Mapper.Test.KeepUnmatched;

using System.Collections.Generic;

public sealed class UnmatchedPrincipal1 : EntityBase
{
    public IList<UnmatchedDependant1> DependantList { get; set; } = null!;
}

public sealed class UnmatchedDependant1 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

public sealed class UnmatchedPrincipal2 : EntityBase
{
    public IList<UnmatchedDependant2> DependantList { get; set; } = null!;
}

public sealed class UnmatchedDependant2 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

namespace Oasis.EntityFramework.Mapper.Test.KeepUnmatched;

using System.Collections.Generic;

public sealed class UnmatchedPrincipal1 : EntityBase
{
    public IList<UnmatchedDependent1> DependentList { get; set; } = null!;
}

public sealed class UnmatchedDependent1 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

public sealed class UnmatchedPrincipal2 : EntityBase
{
    public IList<UnmatchedDependent2> DependentList { get; set; } = null!;
}

public sealed class UnmatchedDependent2 : EntityBase
{
    public long? PrincipalId { get; set; }

    public int IntProp { get; set; }
}

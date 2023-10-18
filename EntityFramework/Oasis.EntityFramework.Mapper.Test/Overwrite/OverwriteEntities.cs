namespace Oasis.EntityFramework.Mapper.Test.Overwrite;

public sealed class OverwriteTargetOuter
{
    public int A { get; set; }

    public long B { get; set; }

    public OverwriteTargetInner Inner { get; set; } = null!;
}

public sealed class OverwriteTargetInner
{
    public float C { get; set; }

    public double D { get; set; }
}

public sealed class OverwriteSourceOuter
{
    public int A { get; set; }

    public OverwriteSourceInner Inner { get; set; } = null!;
}

public sealed class OverwriteSourceInner
{
    public float C { get; set; }
}
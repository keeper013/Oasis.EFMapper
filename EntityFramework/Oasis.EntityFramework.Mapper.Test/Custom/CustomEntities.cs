namespace Oasis.EntityFramework.Mapper.Test.Custom;

public sealed class InterEntity1
{
    public int IntProperty { get; set; }
}

public sealed class CustomEntity1
{
    public string StringProperty { get; set; } = null!;

    public InterEntity1 InternalProperty { get; set; } = null!;
}

public sealed class CustomEntity2
{
    public string StringProperty { get; set; } = null!;

    public int InternalIntProperty { get; set; }
}

public sealed class CustomEntity3
{
    public string StringProperty { get; set; } = null!;

    public int InternalIntProperty { get; set; }

    public InterEntity1 InternalProperty { get; set; } = null!;
}

public sealed class CustomEntity1Wrapper
{
    public long LongProperty { get; set; }

    public CustomEntity1 InnerEntity { get; set; } = null!;
}

public sealed class CustomEntity2Wrapper
{
    public long LongProperty { get; set; }

    public CustomEntity2 InnerEntity { get; set; } = null!;
}
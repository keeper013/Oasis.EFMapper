namespace Oasis.EntityFrameworkCore.Mapper.Test.ExcludedProperties;

public sealed class ExcludedPropertyEntity1 : EntityBase
{
    public int IntProp { get; set; }

    public string? StringProp { get; set; }
}

public sealed class ExcludedPropertyEntity2 : EntityBase
{
    public int IntProp { get; set; }

    public string? StringProp { get; set; }
}
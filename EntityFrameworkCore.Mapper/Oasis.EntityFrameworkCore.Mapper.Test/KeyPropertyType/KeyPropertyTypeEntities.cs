namespace Oasis.EntityFrameworkCore.Mapper.Test.KeyPropertyType;

public sealed class SomeSourceEntity<T> : EntityBase<T>
{
    public SomeSourceEntity()
    {
    }

    public SomeSourceEntity(int prop)
    {
        SomeProperty = prop;
    }

    public int SomeProperty { get; set; }
}

public sealed class SomeTargetEntity<T> : EntityBase<T>
{
    public SomeTargetEntity()
    {
    }

    public SomeTargetEntity(int prop)
    {
        SomeProperty = prop;
    }

    public int SomeProperty { get; set; }
}
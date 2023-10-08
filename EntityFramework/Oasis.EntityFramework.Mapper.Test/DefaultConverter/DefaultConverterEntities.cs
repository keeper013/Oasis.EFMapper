namespace Oasis.EntityFramework.Mapper.Test.DefaultConverter;

public enum DefaultConverterTestEnum
{
    /// <summary>
    /// X.
    /// </summary>
    X,

    /// <summary>
    /// Y.
    /// </summary>
    Y,

    /// <summary>
    /// Z.
    /// </summary>
    Z,
}

public struct DefaultConverterTestStruct
{
    public int X { get; set; }

    public int Y { get; set; }
}

public sealed class DefaultConverterSource<T>
{
    public T Prop { get; set; } = default!;
}

public sealed class DefaultConverterTarget<T>
{
    public T Prop { get; set; } = default!;
}
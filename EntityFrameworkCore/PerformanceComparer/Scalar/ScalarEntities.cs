namespace PerformanceComparer.Scalar;

public abstract class ScalarBase
{
    public int Prop1 { get; set; }
    public long Prop2 { get; set; }
    public short Prop3 { get; set; }
    public byte Prop4 { get; set; }
    public double Prop5 { get; set; }
    public string Prop6 { get; set; } = null!;
    public float Prop7 { get; set; }
    public double Prop8 { get; set; }
    public uint Prop1U { get; set; }
    public ulong Prop2U { get; set; }
    public ushort Prop3U { get; set; }
}

public sealed class ScalarSource : ScalarBase
{
}


public sealed class ScalarTarget : ScalarBase
{
}

public static class ScalarUtilities
{
    public static ScalarSource BuildDefaultScalarSource()
    {
        return new ScalarSource
        {
            Prop1 = 1,
            Prop2 = 2,
            Prop3 = 3,
            Prop4 = 4,
            Prop5 = 'c',
            Prop6 = "Test",
            Prop7 = 1.1f,
            Prop8 = 2.2d,
            Prop1U = 1,
            Prop2U = 2,
            Prop3U = 3,
        };
    }
}

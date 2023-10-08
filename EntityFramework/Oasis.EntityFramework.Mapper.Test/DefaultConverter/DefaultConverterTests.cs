namespace Oasis.EntityFramework.Mapper.Test.DefaultConverter;

using NUnit.Framework;

[TestFixture]
public sealed class DefaultConverterTests : TestBase
{
    [Test]
    public void TestMapping()
    {
        // sbyte
        Test<sbyte, short>(97, 97);
        Test<sbyte, int>(97, 97);
        Test<sbyte, long>(97, 97);
        Test<sbyte, float>(97, 97);
        Test<sbyte, double>(97, 97);
        Test<sbyte, decimal>(97, 97);
        Test<sbyte, sbyte?>(97, 97);
        Test<sbyte, short?>(97, 97);
        Test<sbyte, int?>(97, 97);
        Test<sbyte, long?>(97, 97);
        Test<sbyte, float?>(97, 97);
        Test<sbyte, double?>(97, 97);
        Test<sbyte, decimal?>(97, 97);
        Test<sbyte, string>(97, "97");

        // short
        Test<short, int>(97, 97);
        Test<short, long>(97, 97);
        Test<short, float>(97, 97);
        Test<short, double>(97, 97);
        Test<short, decimal>(97, 97);
        Test<short, short?>(97, 97);
        Test<short, int?>(97, 97);
        Test<short, long?>(97, 97);
        Test<short, float?>(97, 97);
        Test<short, double?>(97, 97);
        Test<short, decimal?>(97, 97);
        Test<short, string>(97, "97");

        // int
        Test<int, long>(97, 97);
        Test<int, double>(97, 97);
        Test<int, decimal>(97, 97);
        Test<int, int?>(97, 97);
        Test<int, long?>(97, 97);
        Test<int, double?>(97, 97);
        Test<int, decimal?>(97, 97);
        Test<int, string>(97, "97");

        // long
        Test<long, double>(97, 97);
        Test<long, decimal>(97, 97);
        Test<long, long?>(97, 97);
        Test<long, double?>(97, 97);
        Test<long, decimal?>(97, 97);
        Test<long, string>(97, "97");

        // sbyte?
        Test<sbyte?, short?>(97, 97);
        Test<sbyte?, int?>(97, 97);
        Test<sbyte?, long?>(97, 97);
        Test<sbyte?, float?>(97, 97);
        Test<sbyte?, double?>(97, 97);
        Test<sbyte?, decimal?>(97, 97);
        Test<sbyte?, string>(97, "97");
        Test<sbyte?, short?>(null, null);
        Test<sbyte?, int?>(null, null);
        Test<sbyte?, long?>(null, null);
        Test<sbyte?, float?>(null, null);
        Test<sbyte?, double?>(null, null);
        Test<sbyte?, decimal?>(null, null);
        Test<sbyte?, string>(null, string.Empty);

        // short?
        Test<short?, int?>(97, 97);
        Test<short?, long?>(97, 97);
        Test<short?, float?>(97, 97);
        Test<short?, double?>(97, 97);
        Test<short?, decimal?>(97, 97);
        Test<short?, string?>(97, "97");
        Test<short?, int?>(null, null);
        Test<short?, long?>(null, null);
        Test<short?, float?>(null, null);
        Test<short?, double?>(null, null);
        Test<short?, decimal?>(null, null);
        Test<short?, string>(null, string.Empty);

        // int?
        Test<int?, long?>(97, 97);
        Test<int?, double?>(97, 97);
        Test<int?, decimal?>(97, 97);
        Test<int?, string>(97, "97");
        Test<int?, long?>(null, null);
        Test<int?, double?>(null, null);
        Test<int?, decimal?>(null, null);
        Test<int?, string>(null, string.Empty);

        // long?
        Test<long?, double?>(97, 97);
        Test<long?, decimal?>(97, 97);
        Test<long?, string>(97, "97");
        Test<long?, double?>(null, null);
        Test<long?, decimal?>(null, null);
        Test<long?, string>(null, string.Empty);

        // byte
        Test<byte, short>(97, 97);
        Test<byte, int>(97, 97);
        Test<byte, long>(97, 97);
        Test<byte, ushort>(97, 97);
        Test<byte, uint>(97, 97);
        Test<byte, ulong>(97, 97);
        Test<byte, float>(97, 97);
        Test<byte, double>(97, 97);
        Test<byte, decimal>(97, 97);
        Test<byte, byte?>(97, 97);
        Test<byte, short?>(97, 97);
        Test<byte, int?>(97, 97);
        Test<byte, long?>(97, 97);
        Test<byte, ushort?>(97, 97);
        Test<byte, uint?>(97, 97);
        Test<byte, ulong?>(97, 97);
        Test<byte, float?>(97, 97);
        Test<byte, double?>(97, 97);
        Test<byte, decimal?>(97, 97);
        Test<byte, string>(97, "97");

        // ushort
        Test<ushort, int>(97, 97);
        Test<ushort, long>(97, 97);
        Test<ushort, uint>(97, 97);
        Test<ushort, ulong>(97, 97);
        Test<ushort, float>(97, 97);
        Test<ushort, double>(97, 97);
        Test<ushort, decimal>(97, 97);
        Test<ushort, int?>(97, 97);
        Test<ushort, long?>(97, 97);
        Test<ushort, ushort?>(97, 97);
        Test<ushort, uint?>(97, 97);
        Test<ushort, ulong?>(97, 97);
        Test<ushort, float?>(97, 97);
        Test<ushort, double?>(97, 97);
        Test<ushort, decimal?>(97, 97);
        Test<ushort, string>(97, "97");

        // uint
        Test<uint, long>(97, 97);
        Test<uint, ulong>(97, 97);
        Test<uint, double>(97, 97);
        Test<uint, decimal>(97, 97);
        Test<uint, long?>(97, 97);
        Test<uint, ulong?>(97, 97);
        Test<uint, double?>(97, 97);
        Test<uint, decimal?>(97, 97);
        Test<uint, string>(97, "97");

        // ulong
        Test<ulong, double>(97, 97);
        Test<ulong, decimal>(97, 97);
        Test<ulong, double?>(97, 97);
        Test<ulong, decimal?>(97, 97);
        Test<ulong, string>(97, "97");

        // byte?
        Test<byte?, short?>(97, 97);
        Test<byte?, int?>(97, 97);
        Test<byte?, long?>(97, 97);
        Test<byte?, ushort?>(97, 97);
        Test<byte?, uint?>(97, 97);
        Test<byte?, ulong?>(97, 97);
        Test<byte?, float?>(97, 97);
        Test<byte?, double?>(97, 97);
        Test<byte?, decimal?>(97, 97);
        Test<byte?, string>(97, "97");
        Test<byte?, short?>(null, null);
        Test<byte?, int?>(null, null);
        Test<byte?, long?>(null, null);
        Test<byte?, ushort?>(null, null);
        Test<byte?, uint?>(null, null);
        Test<byte?, ulong?>(null, null);
        Test<byte?, float?>(null, null);
        Test<byte?, double?>(null, null);
        Test<byte?, decimal?>(null, null);
        Test<byte?, string>(null, string.Empty);

        // ushort?
        Test<ushort?, int?>(97, 97);
        Test<ushort?, long?>(97, 97);
        Test<ushort?, ushort?>(97, 97);
        Test<ushort?, uint?>(97, 97);
        Test<ushort?, ulong?>(97, 97);
        Test<ushort?, float?>(97, 97);
        Test<ushort?, double?>(97, 97);
        Test<ushort?, decimal?>(97, 97);
        Test<ushort?, string>(97, "97");
        Test<ushort?, int?>(null, null);
        Test<ushort?, long?>(null, null);
        Test<ushort?, ushort?>(null, null);
        Test<ushort?, uint?>(null, null);
        Test<ushort?, ulong?>(null, null);
        Test<ushort?, float?>(null, null);
        Test<ushort?, double?>(null, null);
        Test<ushort?, decimal?>(null, null);
        Test<ushort?, string>(null, string.Empty);

        // uint?
        Test<uint?, long?>(97, 97);
        Test<uint?, ulong?>(97, 97);
        Test<uint?, double?>(97, 97);
        Test<uint?, decimal?>(97, 97);
        Test<uint?, string>(97, "97");
        Test<uint?, long?>(null, null);
        Test<uint?, ulong?>(null, null);
        Test<uint?, double?>(null, null);
        Test<uint?, decimal?>(null, null);
        Test<uint?, string>(null, string.Empty);

        // ulong?
        Test<ulong?, double?>(97, 97);
        Test<ulong?, decimal?>(97, 97);
        Test<ulong?, string>(97, "97");
        Test<ulong?, double?>(null, null);
        Test<ulong?, decimal?>(null, null);
        Test<ulong?, string>(null, string.Empty);

        // float
        Test<float, double>(97, 97);
        Test<float, decimal>(97, 97);
        Test<float, double?>(97, 97);
        Test<float, float?>(97, 97);
        Test<float, double?>(97, 97);
        Test<float, string>(97, "97");

        // double
        Test<double, decimal>(97, 97);
        Test<double, double?>(97, 97);
        Test<double, string>(97, "97");

        // decimal
        Test<decimal, decimal?>(97, 97);
        Test<decimal, string>(97, "97");

        // float?
        Test<float?, double?>(97, 97);
        Test<float?, double?>(97, 97);
        Test<float?, string>(97, "97");
        Test<float?, double?>(null, null);
        Test<float?, double?>(null, null);
        Test<float?, string>(null, string.Empty);

        // double?
        Test<double?, string>(97, "97");
        Test<double?, string>(null, string.Empty);
    }

    [Test]
    public void TestEnum()
    {
        Test<DefaultConverterTestEnum, DefaultConverterTestEnum?>(DefaultConverterTestEnum.Y, DefaultConverterTestEnum.Y);
    }

    [Test]
    public void TestStruct()
    {
        var mapper = MakeDefaultMapperBuilder().Register<DefaultConverterSource<DefaultConverterTestStruct>, DefaultConverterTarget<DefaultConverterTestStruct?>>().Build().MakeMapper();
        var source = new DefaultConverterSource<DefaultConverterTestStruct>();
        var prop = default(DefaultConverterTestStruct);
        prop.X = 1;
        prop.Y = 2;
        source.Prop = prop;
        var target = mapper.Map<DefaultConverterSource<DefaultConverterTestStruct>, DefaultConverterTarget<DefaultConverterTestStruct?>>(source);
        Assert.True(target.Prop.HasValue);
        Assert.AreEqual(1, target.Prop!.Value.X);
        Assert.AreEqual(2, target.Prop.Value.Y);
    }

    private static void Test<TSource, TTarget>(TSource sourceValue, TTarget targetValue)
    {
        var mapper = MakeDefaultMapperBuilder().Register<DefaultConverterSource<TSource>, DefaultConverterTarget<TTarget>>().Build().MakeMapper();
        var source = new DefaultConverterSource<TSource> { Prop = sourceValue };
        var target = mapper.Map<DefaultConverterSource<TSource>, DefaultConverterTarget<TTarget>>(source);
        Assert.AreEqual(targetValue, target.Prop);
    }
}

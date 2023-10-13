namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal interface IValueToNullableConverterBuilder
{
    void AddValueTypeToNullableMethod(Type nullableType, Type argumentType);

    bool ContainsValueTypeToNullableConverter(Type sourceType, Type targetType);
}

internal static class DefaultConverterUtilities
{
    private static readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> DefaultConverters = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>
    {
        // signed primitives, non-nullable
        {
            typeof(sbyte), new Dictionary<Type, Delegate>
            {
                { typeof(short), (sbyte s) => (short)s },
                { typeof(int), (sbyte s) => (int)s },
                { typeof(long), (sbyte s) => (long)s },
                { typeof(float), (sbyte s) => (float)s },
                { typeof(double), (sbyte s) => (double)s },
                { typeof(decimal), (sbyte s) => (decimal)s },
                { typeof(sbyte?), (sbyte s) => (sbyte?)s },
                { typeof(short?), (sbyte s) => (short?)s },
                { typeof(int?), (sbyte s) => (int?)s },
                { typeof(long?), (sbyte s) => (long?)s },
                { typeof(float?), (sbyte s) => (float?)s },
                { typeof(double?), (sbyte s) => (double?)s },
                { typeof(decimal?), (sbyte s) => (decimal?)s },
                { typeof(string), (sbyte s) => s.ToString() },
            }
        },
        {
            typeof(short), new Dictionary<Type, Delegate>
            {
                { typeof(int), (short s) => (int)s },
                { typeof(long), (short s) => (long)s },
                { typeof(float), (short s) => (float)s },
                { typeof(double), (short s) => (double)s },
                { typeof(decimal), (short s) => (decimal)s },
                { typeof(short?), (short s) => (short?)s },
                { typeof(int?), (short s) => (int?)s },
                { typeof(long?), (short s) => (long?)s },
                { typeof(float?), (short s) => (float?)s },
                { typeof(double?), (short s) => (double?)s },
                { typeof(decimal?), (short s) => (decimal?)s },
                { typeof(string), (short s) => s.ToString() },
            }
        },
        {
            typeof(int), new Dictionary<Type, Delegate>
            {
                { typeof(long), (int i) => (long)i },
                { typeof(double), (int i) => (double)i },
                { typeof(decimal), (int i) => (decimal)i },
                { typeof(int?), (int i) => (int?)i },
                { typeof(long?), (int i) => (long?)i },
                { typeof(double?), (int i) => (double?)i },
                { typeof(decimal?), (int i) => (decimal?)i },
                { typeof(string), (int i) => i.ToString() },
            }
        },
        {
            typeof(long), new Dictionary<Type, Delegate>
            {
                { typeof(double), (long l) => (double)l },
                { typeof(decimal), (long l) => (decimal)l },
                { typeof(long?), (long l) => (long?)l },
                { typeof(double?), (long l) => (double?)l },
                { typeof(decimal?), (long l) => (decimal?)l },
                { typeof(string), (long l) => l.ToString() },
            }
        },

        // signed primitives, nullable
        {
            typeof(sbyte?), new Dictionary<Type, Delegate>
            {
                { typeof(short?), (sbyte? s) => (short?)s },
                { typeof(int?), (sbyte? s) => (int?)s },
                { typeof(long?), (sbyte? s) => (long?)s },
                { typeof(float?), (sbyte? s) => (float?)s },
                { typeof(double?), (sbyte? s) => (double?)s },
                { typeof(decimal?), (sbyte? s) => (decimal?)s },
                { typeof(string), (sbyte? s) => s.ToString() },
            }
        },
        {
            typeof(short?), new Dictionary<Type, Delegate>
            {
                { typeof(int?), (short? s) => (int?)s },
                { typeof(long?), (short? s) => (long?)s },
                { typeof(float?), (short? s) => (float?)s },
                { typeof(double?), (short? s) => (double?)s },
                { typeof(decimal?), (short? s) => (decimal?)s },
                { typeof(string), (short? s) => s.ToString() },
            }
        },
        {
            typeof(int?), new Dictionary<Type, Delegate>
            {
                { typeof(long?), (int? i) => (long?)i },
                { typeof(double?), (int? i) => (double?)i },
                { typeof(decimal?), (int? i) => (decimal?)i },
                { typeof(string), (int? i) => i.ToString() },
            }
        },
        {
            typeof(long?), new Dictionary<Type, Delegate>
            {
                { typeof(double?), (long? l) => (double?)l },
                { typeof(decimal?), (long? l) => (decimal?)l },
                { typeof(string), (long? l) => l.ToString() },
            }
        },

        // unsigned primitives, non-nullable
        {
            typeof(byte), new Dictionary<Type, Delegate>
            {
                { typeof(short), (byte s) => (short)s },
                { typeof(int), (byte s) => (int)s },
                { typeof(long), (byte s) => (long)s },
                { typeof(ushort), (byte s) => (ushort)s },
                { typeof(uint), (byte s) => (uint)s },
                { typeof(ulong), (byte s) => (ulong)s },
                { typeof(float), (byte s) => (float)s },
                { typeof(double), (byte s) => (double)s },
                { typeof(decimal), (byte s) => (decimal)s },
                { typeof(byte?), (byte s) => (byte?)s },
                { typeof(short?), (byte s) => (short?)s },
                { typeof(int?), (byte s) => (int?)s },
                { typeof(long?), (byte s) => (long?)s },
                { typeof(ushort?), (byte s) => (ushort?)s },
                { typeof(uint?), (byte s) => (uint?)s },
                { typeof(ulong?), (byte s) => (ulong?)s },
                { typeof(float?), (byte s) => (float?)s },
                { typeof(double?), (byte s) => (double?)s },
                { typeof(decimal?), (byte s) => (decimal?)s },
                { typeof(string), (byte s) => s.ToString() },
            }
        },
        {
            typeof(ushort), new Dictionary<Type, Delegate>
            {
                { typeof(int), (ushort s) => (int)s },
                { typeof(long), (ushort s) => (long)s },
                { typeof(uint), (ushort s) => (uint)s },
                { typeof(ulong), (ushort s) => (ulong)s },
                { typeof(float), (ushort s) => (float)s },
                { typeof(double), (ushort s) => (double)s },
                { typeof(decimal), (ushort s) => (decimal)s },
                { typeof(ushort?), (ushort s) => (ushort?)s },
                { typeof(int?), (ushort s) => (int?)s },
                { typeof(long?), (ushort s) => (long?)s },
                { typeof(uint?), (ushort s) => (uint?)s },
                { typeof(ulong?), (ushort s) => (ulong?)s },
                { typeof(float?), (ushort s) => (float?)s },
                { typeof(double?), (ushort s) => (double?)s },
                { typeof(decimal?), (ushort s) => (decimal?)s },
                { typeof(string), (ushort s) => s.ToString() },
            }
        },
        {
            typeof(uint), new Dictionary<Type, Delegate>
            {
                { typeof(long), (uint i) => (long)i },
                { typeof(ulong), (uint i) => (ulong)i },
                { typeof(double), (uint i) => (double)i },
                { typeof(decimal), (uint i) => (decimal)i },
                { typeof(uint?), (uint i) => (uint?)i },
                { typeof(long?), (uint i) => (long?)i },
                { typeof(ulong?), (uint i) => (ulong?)i },
                { typeof(double?), (uint i) => (double?)i },
                { typeof(decimal?), (uint i) => (decimal?)i },
                { typeof(string), (uint i) => i.ToString() },
            }
        },
        {
            typeof(ulong), new Dictionary<Type, Delegate>
            {
                { typeof(double), (ulong l) => (double)l },
                { typeof(decimal), (ulong l) => (decimal)l },
                { typeof(ulong?), (ulong l) => (long?)l },
                { typeof(double?), (ulong l) => (double?)l },
                { typeof(decimal?), (ulong l) => (decimal?)l },
                { typeof(string), (ulong l) => l.ToString() },
            }
        },

        // unsigned primitives, nullable
        {
            typeof(byte?), new Dictionary<Type, Delegate>
            {
                { typeof(short?), (byte? s) => (short?)s },
                { typeof(int?), (byte? s) => (int?)s },
                { typeof(long?), (byte? s) => (long?)s },
                { typeof(ushort?), (byte? s) => (ushort?)s },
                { typeof(uint?), (byte? s) => (uint?)s },
                { typeof(ulong?), (byte? s) => (ulong?)s },
                { typeof(float?), (byte? s) => (float?)s },
                { typeof(double?), (byte? s) => (double?)s },
                { typeof(decimal?), (byte? s) => (decimal?)s },
                { typeof(string), (byte? s) => s.ToString() },
            }
        },
        {
            typeof(ushort?), new Dictionary<Type, Delegate>
            {
                { typeof(int?), (ushort? s) => (int?)s },
                { typeof(long?), (ushort? s) => (long?)s },
                { typeof(uint?), (ushort? s) => (uint?)s },
                { typeof(ulong?), (ushort? s) => (ulong?)s },
                { typeof(float?), (ushort? s) => (float?)s },
                { typeof(double?), (ushort? s) => (double?)s },
                { typeof(decimal?), (ushort? s) => (decimal?)s },
                { typeof(string), (ushort? s) => s.ToString() },
            }
        },
        {
            typeof(uint?), new Dictionary<Type, Delegate>
            {
                { typeof(long?), (uint? i) => (long?)i },
                { typeof(ulong?), (uint? i) => (ulong?)i },
                { typeof(double?), (uint? i) => (double?)i },
                { typeof(decimal?), (uint? i) => (decimal?)i },
                { typeof(string), (uint? i) => i.ToString() },
            }
        },
        {
            typeof(ulong?), new Dictionary<Type, Delegate>
            {
                { typeof(double?), (ulong? l) => (double?)l },
                { typeof(decimal?), (ulong? l) => (decimal?)l },
                { typeof(string), (ulong? l) => l.ToString() },
            }
        },

        // float, double & decimal, non-nullable
        {
            typeof(float), new Dictionary<Type, Delegate>
            {
                { typeof(double), (float f) => (double)f },
                { typeof(float?), (float f) => (float?)f },
                { typeof(double?), (float f) => (double?)f },
                { typeof(string), (float f) => f.ToString() },
            }
        },
        {
            typeof(double), new Dictionary<Type, Delegate>
            {
                { typeof(double?), (double d) => (double?)d },
                { typeof(string), (double d) => d.ToString() },
            }
        },
        {
            typeof(decimal), new Dictionary<Type, Delegate>
            {
                { typeof(decimal?), (decimal d) => (decimal?)d },
                { typeof(string), (decimal d) => d.ToString() },
            }
        },

        // float, double & decimal, nullable
        {
            typeof(float?), new Dictionary<Type, Delegate>
            {
                { typeof(double?), (float? f) => (double?)f },
                { typeof(string), (float? f) => f.ToString() },
            }
        },
        {
            typeof(double?), new Dictionary<Type, Delegate>
            {
                { typeof(string), (double? d) => d.ToString() },
            }
        },
        {
            typeof(decimal?), new Dictionary<Type, Delegate>
            {
                { typeof(string), (decimal? d) => d.ToString() },
            }
        },
    };

    internal static bool TryAddingConverterIfNotExists(
        this Dictionary<Type, Dictionary<Type, Delegate>> converterDictionary,
        Type sourceType,
        Type targetType,
        IValueToNullableConverterBuilder valueToNullableConverterBuilder)
    {
        if (!converterDictionary.Contains(sourceType, targetType) && !valueToNullableConverterBuilder.ContainsValueTypeToNullableConverter(sourceType, targetType))
        {
            var del = DefaultConverters.Find(sourceType, targetType);
            if (del != null)
            {
                converterDictionary.Add(sourceType, targetType, del);
                return true;
            }
            else if (sourceType.IsValueType && targetType.IsNullable(out var argumentType) && sourceType == argumentType)
            {
                valueToNullableConverterBuilder.AddValueTypeToNullableMethod(targetType, sourceType);
            }

            return false;
        }

        return true;
    }
}

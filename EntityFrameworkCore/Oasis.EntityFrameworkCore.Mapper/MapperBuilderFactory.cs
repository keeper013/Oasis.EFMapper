namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;
using System.Security.Cryptography;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder MakeMapperBuilder(
        string? identityPropertyName = default,
        string? concurrencyTokenPropertyName = default,
        string[]? excludedProperties = default,
        bool? keepEntityOnMappingRemoved = default,
        MapToDatabaseType? mapToDatabase = default)
    {
        var excludedProps = excludedProperties != null && excludedProperties.Any() ? new HashSet<string>(excludedProperties) : null;
        return new MapperBuilder(GenerateRandomTypeName(16), identityPropertyName, concurrencyTokenPropertyName, excludedProps, keepEntityOnMappingRemoved, mapToDatabase);
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MakeCustomTypeMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        return new CustomTypeMapperBuilder<TSource, TTarget>();
    }

    private static string GenerateRandomTypeName(int length)
    {
        const string AvailableChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        const int AvailableCharsCount = 52;
        var bytes = RandomNumberGenerator.GetBytes(length);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }
}

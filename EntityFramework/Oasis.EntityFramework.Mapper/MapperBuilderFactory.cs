namespace Oasis.EntityFramework.Mapper;

using Oasis.EntityFramework.Mapper.InternalLogic;
using System.Security.Cryptography;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder MakeMapperBuilder(EntityConfiguration? defaultConfiguration = null)
    {
        return new MapperBuilder(GenerateRandomTypeName(16), defaultConfiguration ?? new EntityConfiguration("Id"));
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
        var bytes = new byte[length];
        RandomNumberGenerator.Create().GetBytes(bytes);
        var str = bytes.Select(b => AvailableChars[b % AvailableCharsCount]);
        return string.Concat(str);
    }
}

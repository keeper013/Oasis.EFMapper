namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder MakeMapperBuilder(string assemblyName, EntityConfiguration? defaultConfiguration = null)
    {
        return new MapperBuilder(assemblyName, defaultConfiguration ?? new EntityConfiguration("Id"));
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MakeCustomTypeMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        return new CustomTypeMapperBuilder<TSource, TTarget>();
    }
}

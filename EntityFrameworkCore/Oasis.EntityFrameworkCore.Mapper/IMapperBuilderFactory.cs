namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder(string assemblyName, EntityConfiguration? defaultConfiguration);

    ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MakeCustomTypeMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class;
}

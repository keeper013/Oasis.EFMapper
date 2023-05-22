namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder(string assemblyName, TypeConfiguration defaultConfiguration);

    ICustomPropertyMapperBuilder<TSource, TTarget> MakeCustomPropertyMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class;
}

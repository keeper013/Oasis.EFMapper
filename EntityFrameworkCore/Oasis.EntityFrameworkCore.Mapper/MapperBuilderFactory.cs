namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder MakeMapperBuilder(string assemblyName, TypeConfiguration? defaultConfiguration)
    {
        return new MapperBuilder(assemblyName, defaultConfiguration ?? new TypeConfiguration("Id"));
    }

    public ICustomPropertyMapperBuilder<TSource, TTarget> MakeCustomPropertyMapperBuilder<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        return new CustomPropertyMapper<TSource, TTarget>();
    }
}

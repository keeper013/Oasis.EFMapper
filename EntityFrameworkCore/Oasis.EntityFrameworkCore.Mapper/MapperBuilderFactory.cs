namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder MakeMapperBuilder(string assemblyName, EntityConfiguration? defaultConfiguration)
    {
        return new MapperBuilder(assemblyName, defaultConfiguration ?? new EntityConfiguration("Id"));
    }

    public ICustomPropertyMapperBuilder<TSource, TTarget> MakeCustomPropertyMapperBuilder<TSource, TTarget>(bool? mappingKeepEntityOnMappingRemoved, IReadOnlyDictionary<string, bool>? propertyKeepEntityOnMappingRemoved)
        where TSource : class
        where TTarget : class
    {
        // TODO: validate property keep entity on mapping removed dictionary, verify if mapping can happen for each named property
        return new CustomPropertyMapper<TSource, TTarget>(mappingKeepEntityOnMappingRemoved, propertyKeepEntityOnMappingRemoved);
    }
}

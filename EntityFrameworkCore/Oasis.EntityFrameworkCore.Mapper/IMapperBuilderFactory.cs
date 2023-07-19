namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder(string assemblyName, EntityConfiguration? defaultConfiguration);

    ICustomPropertyMapperBuilder<TSource, TTarget> MakeCustomPropertyMapperBuilder<TSource, TTarget>(bool? mappingKeepEntityOnMappingRemoved, IReadOnlyDictionary<string, bool>? propertyKeepEntityOnMappingRemoved)
        where TSource : class
        where TTarget : class;
}

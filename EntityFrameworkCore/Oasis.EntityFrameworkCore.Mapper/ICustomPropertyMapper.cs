namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface IPropertyEntityRemover
{
    bool? MappingKeepEntityOnMappingRemoved { get; }

    IReadOnlyDictionary<string, bool>? PropertyKeepEntityOnMappingRemoved { get; }
}

public interface ICustomPropertyMapper
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    Delegate MapProperties { get; }
}

public interface ICustomTypeMapperConfiguration
{
    ICustomPropertyMapper? CustomPropertyMapper { get; }

    IPropertyEntityRemover? PropertyEntityRemover { get; }
}

public interface ICustomTypeMapperConfigurationBuilder<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    ICustomTypeMapperConfigurationBuilder<TSource, TTarget> SetMappingKeepEntityOnMappingRemoved(bool keep);

    ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value);

    ICustomTypeMapperConfigurationBuilder<TSource, TTarget> PropertyKeepEntityOnMappingRemoved(string propertyName, bool keep);

    ICustomTypeMapperConfiguration Build();
}
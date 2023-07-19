namespace Oasis.EntityFrameworkCore.Mapper;

using System.Linq.Expressions;

public interface ICustomPropertyMapperBuilder<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    ICustomPropertyMapperBuilder<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value);

    ICustomPropertyMapper<TSource, TTarget> Build();
}

public interface ICustomPropertyMapper<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    bool? MappingKeepEntityOnMappingRemoved { get; }

    IReadOnlyDictionary<string, bool>? PropertyKeepEntityOnMappingRemoved { get; }

    void MapProperties(TSource source, TTarget target);
}
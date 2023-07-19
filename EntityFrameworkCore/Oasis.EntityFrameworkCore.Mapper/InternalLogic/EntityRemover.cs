namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;

internal sealed class EntityRemover
{
    private readonly bool _defaultKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, bool> _typeKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, IDictionary<Type, bool>> _mappingKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, IDictionary<Type, IDictionary<string, bool>>> _propertyKeepEntityOnMappingRemoved;

    public EntityRemover(
        bool defaultKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, bool> typeKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, IDictionary<Type, bool>> mappingKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, IDictionary<Type, IDictionary<string, bool>>> propertyKeepEntityOnMappingRemoved)
    {
        _defaultKeepEntityOnMappingRemoved = defaultKeepEntityOnMappingRemoved;
        _typeKeepEntityOnMappingRemoved = typeKeepEntityOnMappingRemoved;
        _mappingKeepEntityOnMappingRemoved = mappingKeepEntityOnMappingRemoved;
        _propertyKeepEntityOnMappingRemoved = propertyKeepEntityOnMappingRemoved;
    }

    public void RemoveIfConfigured<TEntity>(DbContext databaseContext, TEntity entity, EntityPropertyMappingData data)
        where TEntity : class
    {
        bool keep = IMapperBuilder.DefaultKeepEntityOnMappingRemoved;
        var propertyConfigured = _propertyKeepEntityOnMappingRemoved.TryGetValue(data.SourceType, out var propertyTarget)
            && propertyTarget.TryGetValue(data.TargetType, out var propertyDict) && propertyDict.TryGetValue(data.PropertyName, out keep);
        if (!propertyConfigured)
        {
            var mappingConfigured = _mappingKeepEntityOnMappingRemoved.TryGetValue(data.SourceType, out var mappingTarget)
                && mappingTarget.TryGetValue(data.TargetType, out keep);
            if (!mappingConfigured)
            {
                var typeConfigured = _typeKeepEntityOnMappingRemoved.TryGetValue(typeof(TEntity), out keep);
                if (!typeConfigured)
                {
                    keep = _defaultKeepEntityOnMappingRemoved;
                }
            }
        }

        if (!keep)
        {
            databaseContext.Set<TEntity>().Remove(entity);
        }
    }
}

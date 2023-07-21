namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Microsoft.EntityFrameworkCore;

internal sealed class EntityRemover
{
    private readonly bool _defaultKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, bool> _typeKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, bool>> _mappingKeepEntityOnMappingRemoved;
    private readonly IReadOnlyDictionary<Type, Dictionary<Type, IReadOnlyDictionary<string, bool>>> _propertyKeepEntityOnMappingRemoved;

    public EntityRemover(
        bool defaultKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, bool> typeKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, Dictionary<Type, bool>> mappingKeepEntityOnMappingRemoved,
        IReadOnlyDictionary<Type, Dictionary<Type, IReadOnlyDictionary<string, bool>>> propertyKeepEntityOnMappingRemoved)
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
        var propertyConfigured = _propertyKeepEntityOnMappingRemoved.TryGetValue(data.sourceType, out var propertyTarget)
            && propertyTarget.TryGetValue(data.targetType, out var propertyDict) && propertyDict.TryGetValue(data.propertyName, out keep);
        if (!propertyConfigured)
        {
            var mappingConfigured = _mappingKeepEntityOnMappingRemoved.TryGetValue(data.sourceType, out var mappingTarget)
                && mappingTarget.TryGetValue(data.targetType, out keep);
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

namespace Oasis.EfMapper;

using Microsoft.EntityFrameworkCore;

public class MappingContext
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;

    public MappingContext(
        DbContext dbContext,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> mappers)
    {
        DbContext = dbContext;
        _mappers = mappers;
    }

    public DbContext DbContext { get; }

    public IDictionary<Type, TargetTracker> TrackerDictionary { get; } = new Dictionary<Type, TargetTracker>();

    public MapperSet? GetMapperSet<TSource, TTarget>()
        where TSource : class, IEntityBase
        where TTarget : class, IEntityBase
    {
        return _mappers.TryGetValue(typeof(TSource), out var innerDictionary)
            && innerDictionary.TryGetValue(typeof(TTarget), out var mapperSet) ?
            mapperSet : null;
    }
}

public class TargetTracker
{
    public ISet<long> IdSet { get; } = new HashSet<long>();

    public ISet<int> HashCodeSet { get; } = new HashSet<int>();
}

public record struct MapperSet(Delegate ScalarPropertiesMapper, Delegate ListPropertiesMapper);

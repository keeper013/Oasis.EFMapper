namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal sealed class MapToDatabaseTypeManager
{
    private readonly MapToDatabaseType _default;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapToDatabaseType>> _dict;

    public MapToDatabaseTypeManager(MapToDatabaseType defaultMapToDatabase, Dictionary<Type, Dictionary<Type, MapToDatabaseType>> dict)
    {
        _default = defaultMapToDatabase;
        var dictionary = new Dictionary<Type, IReadOnlyDictionary<Type, MapToDatabaseType>>();
        foreach (var pair in dict)
        {
            dictionary.Add(pair.Key, pair.Value);
        }

        _dict = dictionary;
    }

    public MapToDatabaseType Get<TSource, TTarget>()
        where TSource : class
        where TTarget : class
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        return _dict.TryGetValue(sourceType, out var innerDictionary) && innerDictionary.TryGetValue(targetType, out var mapToDatabase)
            ? mapToDatabase : _default;
    }
}

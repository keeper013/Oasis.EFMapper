namespace Oasis.EntityFramework.Mapper.InternalLogic;

internal sealed class MapperSetLookUp
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet?>> _mappers;

    public MapperSetLookUp(Dictionary<Type, Dictionary<Type, MapperMetaDataSet?>> dictionary, Type type)
    {
        var mapper = new Dictionary<Type, IReadOnlyDictionary<Type, MapperSet?>>();
        foreach (var pair in dictionary)
        {
            var innerDictionary = new Dictionary<Type, MapperSet?>();
            foreach (var innerPair in pair.Value)
            {
                var mapperMetaDataSet = innerPair.Value;
                if (mapperMetaDataSet.HasValue)
                {
                    var value = mapperMetaDataSet.Value;
                    innerDictionary.Add(innerPair.Key, new MapperSet(
                        value.customPropertiesMapper,
                        value.keyMapper.HasValue ? Delegate.CreateDelegate(value.keyMapper.Value.type, type.GetMethod(value.keyMapper.Value.name)!) : null,
                        value.contentMapper.HasValue ? Delegate.CreateDelegate(value.contentMapper.Value.type, type.GetMethod(value.contentMapper.Value.name)!) : null));
                }
                else
                {
                    innerDictionary.Add(innerPair.Key, null);
                }
            }

            mapper.Add(pair.Key, innerDictionary);
        }

        _mappers = mapper;
    }

    public MapperSet LookUp(Type sourceType, Type targetType)
    {
        return _mappers.Find(sourceType, targetType)!.Value;
    }
}

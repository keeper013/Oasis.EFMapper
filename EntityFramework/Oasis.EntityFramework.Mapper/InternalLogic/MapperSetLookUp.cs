namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;

internal sealed class MapperSetLookUp
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, MapperSet>> _mappers;

    public MapperSetLookUp(Dictionary<Type, Dictionary<Type, MapperMetaDataSet>> dictionary, Type type)
    {
        var mapper = new Dictionary<Type, IReadOnlyDictionary<Type, MapperSet>>();
        foreach (var pair in dictionary)
        {
            var innerDictionary = new Dictionary<Type, MapperSet>();
            foreach (var innerPair in pair.Value)
            {
                var mapperMetaDataSet = innerPair.Value;
                var mapperSet = new MapperSet(
                    mapperMetaDataSet.customPropertiesMapper,
                    Delegate.CreateDelegate(mapperMetaDataSet.keyPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.keyPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.scalarPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.scalarPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.entityPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.entityPropertiesMapper.name)!),
                    Delegate.CreateDelegate(mapperMetaDataSet.listPropertiesMapper.type, type!.GetMethod(mapperMetaDataSet.listPropertiesMapper.name)!));
                innerDictionary.Add(innerPair.Key, mapperSet);
            }

            mapper.Add(pair.Key, innerDictionary);
        }

        _mappers = mapper;
    }

    public MapperSet LookUp(Type sourceType, Type targetType)
    {
        MapperSet mapperSet = default;
        var mapperSetFound = _mappers.TryGetValue(sourceType, out var innerDictionary)
            && innerDictionary.TryGetValue(targetType, out mapperSet);
        if (!mapperSetFound)
        {
            throw new MapperMissingException(sourceType, targetType);
        }

        return mapperSet;
    }
}

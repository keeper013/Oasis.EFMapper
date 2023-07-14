namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

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
                        value.keyPropertiesMapper.HasValue ? Delegate.CreateDelegate(value.keyPropertiesMapper.Value.type, type.GetMethod(value.keyPropertiesMapper.Value.name)!) : null,
                        value.scalarPropertiesMapper.HasValue ? Delegate.CreateDelegate(value.scalarPropertiesMapper.Value.type, type.GetMethod(value.scalarPropertiesMapper.Value.name)!) : null,
                        value.entityPropertiesMapper.HasValue ? Delegate.CreateDelegate(value.entityPropertiesMapper.Value.type, type.GetMethod(value.entityPropertiesMapper.Value.name)!) : null,
                        value.listPropertiesMapper.HasValue ? Delegate.CreateDelegate(value.listPropertiesMapper.Value.type, type.GetMethod(value.listPropertiesMapper.Value.name)!) : null));
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

    public MapperSet? LookUp(Type sourceType, Type targetType)
    {
        MapperSet? mapperSet = default;
        var mapperSetFound = _mappers.TryGetValue(sourceType, out var innerDictionary)
            && innerDictionary.TryGetValue(targetType, out mapperSet);
        if (!mapperSetFound)
        {
            throw new MapperMissingException(sourceType, targetType);
        }

        return mapperSet;
    }
}

namespace Oasis.EntityFrameworkCore.Mapper;

public class MapperFactory : IMapperFactory
{
    public IEntityMapperBuilder Make(string assemblyName)
    {
        return new EntityMapperBuilder(assemblyName);
    }
}

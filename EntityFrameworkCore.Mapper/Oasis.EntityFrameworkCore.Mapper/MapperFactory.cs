namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public class MapperFactory : IMapperFactory
{
    public IEntityMapperBuilder Make(string assemblyName)
    {
        return new EntityMapperBuilder(assemblyName);
    }
}

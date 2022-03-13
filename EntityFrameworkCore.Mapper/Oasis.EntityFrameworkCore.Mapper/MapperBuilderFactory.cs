namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder Make(string assemblyName)
    {
        return new MapperBuilder(assemblyName);
    }
}

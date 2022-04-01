namespace Oasis.EntityFrameworkCore.Mapper;

using Oasis.EntityFrameworkCore.Mapper.InternalLogic;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder Make(string assemblyName, TypeConfiguration defaultConfiguration)
    {
        return new MapperBuilder(assemblyName, defaultConfiguration);
    }
}

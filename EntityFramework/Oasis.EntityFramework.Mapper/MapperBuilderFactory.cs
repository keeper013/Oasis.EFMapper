namespace Oasis.EntityFramework.Mapper;

using Oasis.EntityFramework.Mapper.InternalLogic;

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    public IMapperBuilder Make(string assemblyName, TypeConfiguration defaultConfiguration)
    {
        return new MapperBuilder(assemblyName, defaultConfiguration);
    }
}

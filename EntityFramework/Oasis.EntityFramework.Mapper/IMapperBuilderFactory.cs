namespace Oasis.EntityFramework.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder Make(string assemblyName, TypeConfiguration defaultConfiguration);
}

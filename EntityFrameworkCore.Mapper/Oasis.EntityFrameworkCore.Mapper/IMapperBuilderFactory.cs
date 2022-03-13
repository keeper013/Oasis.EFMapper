namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder Make(string assemblyName);
}

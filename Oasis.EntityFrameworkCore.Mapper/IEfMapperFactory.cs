namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperFactory
{
    IEntityMapperBuilder Make(string assemblyName);
}

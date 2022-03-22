namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    // TODO: integrate with .net core pipeline
    IMapperBuilder Make(string assemblyName);
}

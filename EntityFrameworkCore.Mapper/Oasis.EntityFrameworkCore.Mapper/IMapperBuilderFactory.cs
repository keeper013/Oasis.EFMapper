namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    // TODO: consider to integrate with .net core pipeline
    IMapperBuilder Make(string assemblyName);
}

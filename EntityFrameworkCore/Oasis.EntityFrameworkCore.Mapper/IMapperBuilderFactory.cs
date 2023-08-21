namespace Oasis.EntityFrameworkCore.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder();

    IMapperBuilderConfigurationBuilder Configure();
}
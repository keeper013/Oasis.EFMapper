namespace Oasis.EntityFramework.Mapper;

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder();

    IMapperBuilderConfigurationBuilder Configure();
}
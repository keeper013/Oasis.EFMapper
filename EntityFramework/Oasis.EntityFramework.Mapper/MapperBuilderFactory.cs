namespace Oasis.EntityFramework.Mapper;

using Oasis.EntityFramework.Mapper.InternalLogic;
using System.Security.Cryptography;

public interface IConfigurator<TConfigurator>
    where TConfigurator : class
{
    TConfigurator Finish();
}

public abstract class BuilderConfiguration<TConfigurator, TInterface> : IConfigurator<TInterface>
    where TInterface : class
    where TConfigurator : TInterface
{
    private readonly TConfigurator _configurator;

    protected BuilderConfiguration(TConfigurator configurator)
    {
        _configurator = configurator;
    }

    public TInterface Finish()
    {
        Configure(_configurator);
        return _configurator;
    }

    protected abstract void Configure(TConfigurator configurator);
}

public sealed class MapperBuilderFactory : IMapperBuilderFactory
{
    private MapperBuilderConfigurationBuilder? _configuration;

    public IMapperBuilder MakeMapperBuilder()
    {
        return new MapperBuilder(Utilities.GenerateRandomName(16), _configuration);
    }

    public IMapperBuilderConfigurationBuilder Configure()
    {
        return _configuration ??= new MapperBuilderConfigurationBuilder(this);
    }
}

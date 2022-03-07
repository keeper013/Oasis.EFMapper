namespace Oasis.EfMapper;

public interface IEfMapperFactory
{
    IEntityMapperBuilder Make(string assemblyName);
}

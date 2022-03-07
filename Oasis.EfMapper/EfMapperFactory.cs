namespace Oasis.EfMapper;

public class EfMapperFactory : IEfMapperFactory
{
    public IEntityMapperBuilder Make(string assemblyName)
    {
        return new EntityMapperBuilder(assemblyName);
    }
}

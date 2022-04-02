namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class AsNoTrackingNotAllowedException : EfCoreMapperException
{
    public AsNoTrackingNotAllowedException(string includerString)
        : base($"{includerString}: Call of AsNoTracking() method is not allowed when mapping to entities to avoid sub entity deletion failures.")
    {
    }
}

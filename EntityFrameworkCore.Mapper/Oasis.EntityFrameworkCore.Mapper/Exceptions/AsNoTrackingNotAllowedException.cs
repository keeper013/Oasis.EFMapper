namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class AsNoTrackingNotAllowedException : Exception
{
    private readonly string _includerString;

    public AsNoTrackingNotAllowedException(string includerString)
    {
        _includerString = includerString;
    }

    public override string Message => $"{_includerString}: Call of AsNoTracking() method is not allowed when mapping to entities to avoid sub entity deletion failures.";
}

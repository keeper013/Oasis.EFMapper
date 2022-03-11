namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class MappingContextNotStartedException : Exception
{
    public override string Message => "A mapping context hasn't been started";
}

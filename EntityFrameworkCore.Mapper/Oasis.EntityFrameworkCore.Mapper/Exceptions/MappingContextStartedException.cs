namespace Oasis.EntityFrameworkCore.Mapper.Exceptions
{
    public class MappingContextStartedException : Exception
    {
        public override string Message => "A mapping context has been started, no other mapping context can start before it ends.";
    }
}

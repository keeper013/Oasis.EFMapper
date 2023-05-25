namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public class MapperExistsException : EfCoreMapperException
{
    public MapperExistsException(string sourceType, string targetType)
        : base($"Mapping from {sourceType} to {targetType} has been done without a custom property mapper, maybe by resursive registration, you may want to adjust the mapper registration order.")
    {
    }
}

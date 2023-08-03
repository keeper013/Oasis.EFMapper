namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

public sealed class DuplicatedListItemException : EfCoreMapperException
{
    public DuplicatedListItemException(Type type)
        : base($"Identical items of type {type.Name} found in entity list property.")
    {
    }
}

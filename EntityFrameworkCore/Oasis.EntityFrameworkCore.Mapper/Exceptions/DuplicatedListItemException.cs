namespace Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class DuplicatedListItemException : EfCoreMapperException
{
    public DuplicatedListItemException(Type type)
        : base($"Identical items of type {type} found in entity list property.")
    {
    }
}

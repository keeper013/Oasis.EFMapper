namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class DuplicatedListItemException : EfMapperException
{
    public DuplicatedListItemException(Type type)
        : base($"Identical items of type {type} found in entity list property.")
    {
    }
}

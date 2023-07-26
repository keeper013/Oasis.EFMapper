﻿namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class UnconstructableTypeException : EfMapperException
{
    public UnconstructableTypeException(Type type)
        : base($"Type {type} doesn't have a parameterless constructor or a factory method defined.")
    {
    }
}

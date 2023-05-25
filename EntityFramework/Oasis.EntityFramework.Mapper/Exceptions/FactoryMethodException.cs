﻿namespace Oasis.EntityFramework.Mapper.Exceptions;

public sealed class FactoryMethodExistsException : EfMapperException
{
    public FactoryMethodExistsException(Type type)
        : base($"Type {type} already has a factory method registered.")
    {
    }
}

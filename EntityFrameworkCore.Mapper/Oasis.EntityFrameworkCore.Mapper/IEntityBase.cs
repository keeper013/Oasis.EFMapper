﻿namespace Oasis.EntityFrameworkCore.Mapper;

// TODO: make id and timestamp time generic
public interface IEntityBase
{
    public long? Id { get; }

    public byte[]? Timestamp { get; }
}
namespace Oasis.EntityFrameworkCore.Mapper;

[Flags]
public enum MapToDatabaseType : byte
{
    /// <summary>
    /// Neither insert nor update is allowed
    /// </summary>
    None = 0,

    /// <summary>
    /// Insert
    /// </summary>
    Insert = 1,

    /// <summary>
    /// Update
    /// </summary>
    Update = 2,

    /// <summary>
    /// Insert or Update
    /// </summary>
    Upsert = Insert | Update,
}

public interface IMapperBuilderFactory
{
    IMapperBuilder MakeMapperBuilder();

    IMapperBuilderConfigurationBuilder Configure();
}
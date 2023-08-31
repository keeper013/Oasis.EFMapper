namespace EfMapperDemo;

public abstract class Entity
{
    public string Name { get; set; } = null!;
}

public sealed class Employee : Entity
{
    public string Description { get; set; } = null!;

    public string? ProjectName { get; set; }
}

public sealed class Project : Entity
{
    public string Description { get; set; } = null!;

    public ICollection<Employee> Employees { get; set; } = null!;
}

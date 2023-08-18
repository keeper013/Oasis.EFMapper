namespace Oasis.EntityFrameworkCore.Mapper.Test.ManyToMany;

using System.Collections.Generic;

public sealed class ManyToManyParent1 : EntityBase
{
    public string Name { get; set; } = null!;

    public IList<ManyToManyChild1> Children { get; set; } = new List<ManyToManyChild1>();
}

public sealed class ManyToManyChild1 : EntityBase
{
    public string Name { get; set; } = null!;

    public IList<ManyToManyParent1> Parents { get; set; } = new List<ManyToManyParent1>();
}

public sealed class ManyToManyParent2 : EntityBase
{
    public string Name { get; set; } = null!;

    public IList<ManyToManyChild2> Children { get; set; } = new List<ManyToManyChild2>();
}

public sealed class ManyToManyChild2 : EntityBase
{
    public string Name { get; set; } = null!;

    public IList<ManyToManyParent2> Parents { get; set; } = new List<ManyToManyParent2>();
}
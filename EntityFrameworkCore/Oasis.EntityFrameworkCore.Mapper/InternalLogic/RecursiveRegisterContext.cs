namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal abstract class RecursiveContext
{
    protected Stack<(Type, Type)> Stack { get; } = new ();

    public void Push(Type sourceType, Type targetType) => Stack.Push((sourceType, targetType));

    public void Pop() => Stack.Pop();

    public bool Contains(Type sourceType, Type targetType) => Stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType);
}

internal sealed class RecursiveContextPopper : IDisposable
{
    private readonly RecursiveContext? _context;

    public RecursiveContextPopper(RecursiveContext? context, Type sourceType, Type targetType)
    {
        _context = context;
        _context?.Push(sourceType, targetType);
    }

    public void Dispose()
    {
        _context?.Pop();
    }
}

internal sealed class RecursiveRegisterContext : RecursiveContext
{
    private static readonly MethodInfo RecursivelyRegisterMethod = typeof(MapperRegistry).GetMethod("RecursivelyRegister", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping;

    public RecursiveRegisterContext(Dictionary<Type, ISet<Type>> loopDependencyMapping)
    {
        _loopDependencyMapping = loopDependencyMapping;
    }

    public void DumpLoopDependency()
    {
        foreach (var mappingTuple in Stack)
        {
            if (_loopDependencyMapping.TryGetValue(mappingTuple.Item1, out var set))
            {
                set.Add(mappingTuple.Item2);
            }
            else
            {
                _loopDependencyMapping.Add(mappingTuple.Item1, new HashSet<Type> { mappingTuple.Item2 });
            }
        }
    }

    public void RegisterIf(MapperRegistry mapperRegistry, Type sourceType, Type targetType, bool hasRegistered)
    {
        if (hasRegistered)
        {
            if (_loopDependencyMapping.TryGetValue(sourceType, out var set) && set.Contains(targetType))
            {
                DumpLoopDependency();
            }
        }
        else
        {
            // there is no way to get generic arguments that user defined for the source and target,
            // so though reflection is slow, it's the only way that works here.
            // considering registering is a one-time-job, it's acceptable.
            RecursivelyRegisterMethod.Invoke(mapperRegistry, new object?[] { sourceType, targetType, this });
        }
    }
}

internal sealed class MappingToDatabaseContext : RecursiveContext
{
    public EntityPropertyMappingData MakeMappingData(string propertyName)
    {
        var tuple = Stack.Peek();
        return new EntityPropertyMappingData(tuple.Item1, tuple.Item2, propertyName);
    }
}
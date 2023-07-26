namespace Oasis.EntityFramework.Mapper.InternalLogic;

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

internal interface IRecursiveRegister
{
    void RecursivelyRegister(Type sourceType, Type targetType, RecursiveRegisterContext context);

    void RegisterEntityListDefaultConstructorMethod(Type type);
}

internal sealed class RecursiveRegisterContext : RecursiveContext
{
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping;
    private readonly ISet<Type> _targetsToBeTracked;

    public RecursiveRegisterContext(Dictionary<Type, ISet<Type>> loopDependencyMapping, ISet<Type> targetsToBeTracked)
    {
        _loopDependencyMapping = loopDependencyMapping;
        _targetsToBeTracked = targetsToBeTracked;
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

    public void DumpTargetsToBeTracked()
    {
        foreach (var mappingTuple in Stack)
        {
            _targetsToBeTracked.Add(mappingTuple.Item2);
        }
    }

    public void RegisterIf(IRecursiveRegister recursiveRegister, Type sourceType, Type targetType, bool hasRegistered)
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
            recursiveRegister.RecursivelyRegister(sourceType, targetType, this);
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
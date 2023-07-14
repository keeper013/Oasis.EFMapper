namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface IRecursiveRegisterTrigger
{
    void RegisterIf(Type sourceType, Type targetType, bool hasRegistered);
}

internal sealed class RecursiveRegisterContext : IRecursiveRegisterTrigger
{
    private static readonly MethodInfo RecursivelyRegisterMethod = typeof(MapperRegistry).GetMethod("RecursivelyRegister", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly Stack<(Type, Type)> _stack = new ();
    private readonly MapperRegistry _mapperRegistry;
    private readonly Dictionary<Type, ISet<Type>> _loopDependencyMapping;

    public RecursiveRegisterContext(MapperRegistry mapperRegistry, IDynamicMethodBuilder methodBuilder, Dictionary<Type, ISet<Type>> loopDependencyMapping)
    {
        _mapperRegistry = mapperRegistry;
        _loopDependencyMapping = loopDependencyMapping;
        MethodBuilder = methodBuilder;
    }

    public IDynamicMethodBuilder MethodBuilder { get; }

    public void Push(Type sourceType, Type targetType) => _stack.Push((sourceType, targetType));

    public void Pop() => _stack.Pop();

    public void DumpLoopDependency()
    {
        foreach (var mappingTuple in _stack)
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

    public bool Contains(Type sourceType, Type targetType) => _stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType);

    public void RegisterIf(Type sourceType, Type targetType, bool hasRegistered)
    {
        if (!_stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            if (hasRegistered)
            {
                if (LoopDependencyContains(sourceType, targetType))
                {
                    DumpLoopDependency();
                }
            }
            else
            {
                // there is no way to get generic arguments that user defined for the source and target,
                // so though reflection is slow, it's the only way that works here.
                // considering registering is a one-time-job, it's acceptable.
                RecursivelyRegisterMethod.Invoke(_mapperRegistry, new object?[] { sourceType, targetType, this, null });
            }
        }
        else
        {
            DumpLoopDependency();
        }
    }

    private bool LoopDependencyContains(Type sourceType, Type targetType) => _loopDependencyMapping.TryGetValue(sourceType, out var set) && set.Contains(targetType);
}

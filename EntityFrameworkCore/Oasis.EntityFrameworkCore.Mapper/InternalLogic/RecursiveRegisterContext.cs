namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

internal interface IRecursiveRegisterTrigger
{
    void RegisterIf(Type sourceType, Type targetType, bool condition);
}

internal sealed class RecursiveRegisterContext : IRecursiveRegisterTrigger
{
    private static readonly MethodInfo RecursivelyRegisterMethod = typeof(MapperRegistry).GetMethod("RecursivelyRegister", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly Stack<(Type, Type)> _stack = new ();
    private readonly MapperRegistry _mapperRegistry;

    public RecursiveRegisterContext(MapperRegistry mapperRegistry, IDynamicMethodBuilder methodBuilder)
    {
        _mapperRegistry = mapperRegistry;
        MethodBuilder = methodBuilder;
    }

    public IDynamicMethodBuilder MethodBuilder { get; }

    public void Push(Type sourceType, Type targetType) => _stack.Push((sourceType, targetType));

    public void Pop() => _stack.Pop();

    public bool Contains(Type sourceType, Type targetType) => _stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType);

    public void RegisterIf(Type sourceType, Type targetType, bool condition)
    {
        if (condition && !_stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            // there is no way to get generic arguments that user defined for the source and target,
            // so though reflection is slow, it's the only way that works here.
            // considering registering is a one-time-job, it's acceptable.
            RecursivelyRegisterMethod.Invoke(_mapperRegistry, new object[] { sourceType, targetType, this });
        }
    }
}

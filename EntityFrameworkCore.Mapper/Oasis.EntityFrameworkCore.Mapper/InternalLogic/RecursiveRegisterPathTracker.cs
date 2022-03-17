namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Reflection;

internal class RecursiveRegisterPathTracker
{
    private static readonly MethodInfo RecursivelyRegisterMethod = typeof(MapperBuilder).GetMethod("RecursivelyRegister", Utilities.NonPublicInstance)!;
    private readonly Stack<(Type, Type)> _stack = new Stack<(Type, Type)>();
    private readonly MapperBuilder _mapperBuilder;

    public RecursiveRegisterPathTracker(MapperBuilder mapperBuilder)
    {
        _mapperBuilder = mapperBuilder;
    }

    public void Push(Type sourceType, Type targetType) => _stack.Push((sourceType, targetType));

    public void Pop() => _stack.Pop();

    public bool Contains(Type sourceType, Type targetType) => _stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType);

    public void RegisterIf(Type sourceType, Type targetType, bool condition)
    {
        if (condition && !_stack.Any(i => i.Item1 == sourceType && i.Item2 == targetType))
        {
            var recursivelyRegisterMethod = RecursivelyRegisterMethod.MakeGenericMethod(sourceType, targetType);
            recursivelyRegisterMethod.Invoke(_mapperBuilder, new object[] { this });
        }
    }
}

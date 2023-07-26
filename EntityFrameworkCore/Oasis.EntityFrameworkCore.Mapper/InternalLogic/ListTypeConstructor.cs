namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal interface IListTypeConstructor
{
    TList Construct<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class;
}

internal sealed class ListTypeConstructor : IListTypeConstructor
{
    private static readonly Type[] ListTypes = new[] { typeof(ICollection<>), typeof(IList<>), typeof(List<>) };
    private readonly IDictionary<Type, Delegate> _factoryMethods;
    private readonly Dictionary<Type, Delegate> _generatedConstructors = new ();

    public ListTypeConstructor(IDictionary<Type, Delegate> factoryMethods, IReadOnlyDictionary<Type, MethodMetaData> generatedConstructors, Type type)
    {
        _factoryMethods = factoryMethods;
        foreach (var kvp in generatedConstructors)
        {
            _generatedConstructors.Add(kvp.Key, Delegate.CreateDelegate(kvp.Value.type, type.GetMethod(kvp.Value.name)!));
        }
    }

    public TList Construct<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        if (_factoryMethods.TryGetValue(typeof(TList), out var @delegate))
        {
            return ((Func<TList>)@delegate)();
        }

        var listType = typeof(TList);
        if (listType.IsGenericType)
        {
            if (ListTypes.Contains(listType.GetGenericTypeDefinition()))
            {
                _factoryMethods.Add(listType, CreateList<TList, TItem>);
                return CreateList<TList, TItem>();
            }
        }
        else if (listType.IsConstructable() && _generatedConstructors.TryGetValue(typeof(TList), out var generatedConstructor))
        {
            return ((Func<TList>)generatedConstructor)();
        }

        throw new InvalidOperationException($"Type {typeof(TList)} doesn't have custom either factory method or a generated construct method.");
    }

    private static TList CreateList<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return (new List<TItem>() as TList)!;
    }
}

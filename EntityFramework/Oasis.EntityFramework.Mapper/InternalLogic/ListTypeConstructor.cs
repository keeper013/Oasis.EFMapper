namespace Oasis.EntityFramework.Mapper.InternalLogic;

using Oasis.EntityFramework.Mapper.Exceptions;

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

    public ListTypeConstructor(Dictionary<Type, Delegate> factoryMethods)
    {
        _factoryMethods = factoryMethods;
    }

    TList IListTypeConstructor.Construct<TList, TItem>()
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
        else if (listType.IsClass && !listType.IsAbstract && listType.GetConstructor(Utilities.PublicInstance, null, new Type[0], null) != default)
        {
            _factoryMethods.Add(listType, ActivateType<TList>);
            return ActivateType<TList>(listType);
        }

        throw new UnknownListTypeException(listType);
    }

    private static TList CreateList<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return (new List<TItem>() as TList)!;
    }

    private static TList ActivateType<TList>(Type listType)
    {
        return (TList)Activator.CreateInstance(listType)!;
    }
}

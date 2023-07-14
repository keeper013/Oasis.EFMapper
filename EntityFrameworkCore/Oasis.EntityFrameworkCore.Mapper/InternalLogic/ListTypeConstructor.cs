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
        else if (listType.IsClass && !listType.IsAbstract)
        {
            var constructorInfo = listType.GetConstructor(Utilities.PublicInstance, Array.Empty<Type>());
            if (constructorInfo != null)
            {
                Func<TList> func = () => (TList)constructorInfo.Invoke(Array.Empty<object>());
                _factoryMethods.Add(listType, func);
                return func();
            }
        }

        throw new UnconstructableTypeException(listType);
    }

    private static TList CreateList<TList, TItem>()
        where TList : class, ICollection<TItem>
        where TItem : class
    {
        return (new List<TItem>() as TList)!;
    }
}

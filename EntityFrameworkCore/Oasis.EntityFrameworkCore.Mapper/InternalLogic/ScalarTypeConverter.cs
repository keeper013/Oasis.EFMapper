namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;

internal sealed class ScalarTypeConverter : IScalarTypeConverter
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> _scalarConverterDictionary;

    public ScalarTypeConverter(Dictionary<Type, Dictionary<Type, Delegate>> scalarConverterDictionary)
    {
        var dictionary = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var pair in scalarConverterDictionary)
        {
            dictionary.Add(pair.Key, pair.Value);
        }

        _scalarConverterDictionary = dictionary;
    }

    public TTarget Convert<TSource, TTarget>(TSource source)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);
        if (!_scalarConverterDictionary.TryGetValue(sourceType, out var innerDictionary) || !innerDictionary.TryGetValue(targetType, out var converter))
        {
            throw new ScalarConverterMissingException(sourceType, targetType);
        }

        return ((Func<TSource, TTarget>)converter)(source);
    }
}

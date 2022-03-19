namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

internal class ScalarConverterCache
{
    private readonly Dictionary<Type, Dictionary<Type, Delegate>> _scalarConverterDictionary = new ();
    private readonly HashSet<Type> _convertableToScalarSourceTypes = new ();
    private readonly HashSet<Type> _convertableToScalarTargetTypes = new ();

    public IReadOnlySet<Type> SourceTypes => _convertableToScalarSourceTypes;

    public IReadOnlySet<Type> TargetTypes => _convertableToScalarTargetTypes;

    public void Register<TSource, TTarget>(Expression<Func<TSource, TTarget>> expression)
    {
        var sourceType = typeof(TSource);
        var targetType = typeof(TTarget);

        var sourceIsNotScalarType = !Utilities.IsScalarType(sourceType);
        var targetIsNotScalarType = !Utilities.IsScalarType(targetType);
        if (sourceIsNotScalarType && targetIsNotScalarType)
        {
            throw new ScalarTypeMissingException(sourceType, targetType);
        }

        if (!_scalarConverterDictionary.TryGetValue(sourceType, out var innerDictionary))
        {
            innerDictionary = new Dictionary<Type, Delegate>();
            _scalarConverterDictionary[sourceType] = innerDictionary;
        }

        if (!innerDictionary.ContainsKey(targetType))
        {
            innerDictionary.Add(targetType, expression.Compile());
            if (sourceIsNotScalarType)
            {
                _convertableToScalarSourceTypes.Add(sourceType);
            }
            else if (targetIsNotScalarType)
            {
                _convertableToScalarTargetTypes.Add(targetType);
            }
        }
        else
        {
            throw new ScalarMapperExistsException(sourceType, targetType);
        }
    }

    public IReadOnlyDictionary<Type, IReadOnlyDictionary<Type, Delegate>> Export()
    {
        var scalarConverter = new Dictionary<Type, IReadOnlyDictionary<Type, Delegate>>();
        foreach (var pair in _scalarConverterDictionary)
        {
            scalarConverter.Add(pair.Key, pair.Value);
        }

        return scalarConverter;
    }

    public bool Contains(Type sourceType, Type targetType) => _scalarConverterDictionary.ItemExists(sourceType, targetType);
}

namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using System.Linq.Expressions;

internal sealed class PropertyEntityRemover : IPropertyEntityRemover
{
    private readonly Dictionary<string, bool> _propertyKeepEntityOnMappingRemoved = new ();
    private bool? _mappingKeepEntityOnMappingRemoved;

    public bool? MappingKeepEntityOnMappingRemoved => _mappingKeepEntityOnMappingRemoved;

    public IReadOnlyDictionary<string, bool>? PropertyKeepEntityOnMappingRemoved => _propertyKeepEntityOnMappingRemoved.Any() ? _propertyKeepEntityOnMappingRemoved : null;

    public bool HasContent => _mappingKeepEntityOnMappingRemoved.HasValue || _propertyKeepEntityOnMappingRemoved.Any();

    public bool KeepEntityOnMappingRemoved { set => _mappingKeepEntityOnMappingRemoved = value; }

    public void KeepEntityOnMappingRemovedForProperty(string propertyName, bool keep)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        if (_propertyKeepEntityOnMappingRemoved.ContainsKey(propertyName))
        {
            throw new ArgumentException($"Property ${propertyName} has been configured for KeepEntityOnMappingRemoved.", nameof(propertyName));
        }

        _propertyKeepEntityOnMappingRemoved.Add(propertyName, keep);
    }
}

internal class CustomPropertyMapper<TSource, TTarget> : ICustomPropertyMapper
    where TSource : class
    where TTarget : class
{
    private readonly IList<Action<TSource, TTarget>> _propertyMappingFunctions = new List<Action<TSource, TTarget>>();
    private readonly ISet<PropertyInfo> _mappedProperties = new HashSet<PropertyInfo>();

    public bool HasContent => _propertyMappingFunctions.Any();

    public IEnumerable<PropertyInfo> MappedTargetProperties => _mappedProperties.ToList();

    public Delegate MapProperties => MapPropertiesFunc;

    public void MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value)
    {
        var setterAction = CreateSetter(setter);
        var valueFunc = value.Compile();
        _propertyMappingFunctions.Add((source, target) => setterAction(target, valueFunc(source)));
    }

    private static PropertyInfo GetProperty<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        var member = GetMemberExpression(expression).Member;
        var property = member as PropertyInfo;
        if (property == null)
        {
            throw new InvalidOperationException(string.Format("Member with Name '{0}' is not a property.", member.Name));
        }

        return property;
    }

    private static MemberExpression GetMemberExpression<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> expression)
    {
        MemberExpression? memberExpression = null;
        if (expression.Body.NodeType == ExpressionType.Convert)
        {
            var body = (UnaryExpression)expression.Body;
            memberExpression = body.Operand as MemberExpression;
        }
        else if (expression.Body.NodeType == ExpressionType.MemberAccess)
        {
            memberExpression = expression.Body as MemberExpression;
        }

        if (memberExpression == null)
        {
            throw new ArgumentException("Not a member access", nameof(expression));
        }

        return memberExpression;
    }

    private void MapPropertiesFunc(TSource source, TTarget target)
    {
        foreach (var func in _propertyMappingFunctions)
        {
            func(source, target);
        }
    }

    private Action<TEntity, TProperty> CreateSetter<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> memberAccess)
    {
        PropertyInfo propertyInfo = GetProperty(memberAccess);
        if (!_mappedProperties.Add(propertyInfo))
        {
            throw new ArgumentException($"Property {propertyInfo.Name} has been mapped for custom property mapper from ${typeof(TSource).Name} to {typeof(TTarget).Name}", nameof(memberAccess));
        }

        ParameterExpression instance = Expression.Parameter(typeof(TEntity), "instance");
        ParameterExpression parameter = Expression.Parameter(typeof(TProperty), "param");

        var setMethod = propertyInfo.GetSetMethod();
        if (setMethod == null)
        {
            throw new ArgumentException("Not a setter", nameof(memberAccess));
        }

        var body = Expression.Call(instance, setMethod, parameter);
        var parameters = new ParameterExpression[] { instance, parameter };

        return Expression.Lambda<Action<TEntity, TProperty>>(body, parameters).Compile();
    }
}

internal class CustomTypeMapperBuilder<TSource, TTarget> : ICustomTypeMapperConfigurationBuilder<TSource, TTarget>, ICustomTypeMapperConfiguration<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    private CustomPropertyMapper<TSource, TTarget> _customPropertyMapper = new ();
    private PropertyEntityRemover _propertyEntityRemover = new ();
    private ISet<string> _excludedProperties = new HashSet<string>();

    public string[]? ExcludedProperties => _excludedProperties.Any() ? _excludedProperties.ToArray() : default;

    public ICustomPropertyMapper? CustomPropertyMapper => _customPropertyMapper.HasContent ? _customPropertyMapper : default;

    public IPropertyEntityRemover? PropertyEntityRemover => _propertyEntityRemover.HasContent ? _propertyEntityRemover : default;

    public MapToDatabaseType? MapToDatabaseType { get; private set; }

    public ICustomTypeMapperConfiguration<TSource, TTarget> Build()
    {
        return this;
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value)
    {
        _customPropertyMapper.MapProperty(setter, value);
        return this;
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> PropertyKeepEntityOnMappingRemoved(string propertyName, bool keep)
    {
        _propertyEntityRemover.KeepEntityOnMappingRemovedForProperty(propertyName, keep);
        return this;
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> SetMappingKeepEntityOnMappingRemoved(bool keep)
    {
        _propertyEntityRemover.KeepEntityOnMappingRemoved = keep;
        return this;
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> ExcludePropertyByName(params string[] names)
    {
        _excludedProperties.UnionWith(names);
        return this;
    }

    public ICustomTypeMapperConfigurationBuilder<TSource, TTarget> SetMapToDatabaseType(MapToDatabaseType mapToDatabase)
    {
        MapToDatabaseType = mapToDatabase;
        return this;
    }
}

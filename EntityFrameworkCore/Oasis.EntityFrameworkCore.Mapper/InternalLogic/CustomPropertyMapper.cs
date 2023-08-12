namespace Oasis.EntityFrameworkCore.Mapper.InternalLogic;

using Oasis.EntityFrameworkCore.Mapper.Exceptions;
using System.Linq.Expressions;

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
            throw new ArgumentException($"Property {propertyInfo.Name} has been mapped for custom property mapper from {typeof(TSource).Name} to {typeof(TTarget).Name}", nameof(memberAccess));
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

internal class CustomTypeMapperBuilder<TSource, TTarget> : BuilderConfiguration<MapperBuilder, IMapperBuilder>, ICustomTypeMapperConfiguration<TSource, TTarget>, ICustomTypeMapperConfiguration
    where TSource : class
    where TTarget : class
{
    private CustomPropertyMapper<TSource, TTarget> _customPropertyMapper = new ();

    public CustomTypeMapperBuilder(MapperBuilder builder)
        : base(builder)
    {
    }

    public IReadOnlySet<string>? ExcludedProperties { get; set; }

    public IReadOnlySet<string>? KeepUnmatchedProperties { get; set; }

    public ICustomPropertyMapper? CustomPropertyMapper => _customPropertyMapper.HasContent ? _customPropertyMapper : default;

    public MapToDatabaseType? MapToDatabaseType { get; private set; }

    public ICustomTypeMapperConfiguration<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value)
    {
        _customPropertyMapper.MapProperty(setter, value);
        return this;
    }

    public ICustomTypeMapperConfiguration<TSource, TTarget> ExcludePropertiesByName(params string[] names)
    {
        if (names != null && names.Any())
        {
            var sourceProperties = typeof(TSource).GetProperties(Utilities.PublicInstance);
            var targetProperties = typeof(TTarget).GetProperties(Utilities.PublicInstance);
            foreach (var propertyName in names)
            {
                if (!sourceProperties.Any(p => string.Equals(p.Name, propertyName)) || !targetProperties.Any(p => string.Equals(p.Name, propertyName)))
                {
                    throw new UselessExcludeException(typeof(TSource), typeof(TTarget), propertyName);
                }
            }

            ExcludedProperties = new HashSet<string>(names);
        }

        return this;
    }

    public ICustomTypeMapperConfiguration<TSource, TTarget> KeepUnmatched(params string[] names)
    {
        if (names != null && names.Any())
        {
            var sourceProperties = typeof(TSource).GetProperties(Utilities.PublicInstance);
            var targetProperties = typeof(TTarget).GetProperties(Utilities.PublicInstance);
            foreach (var propertyName in names)
            {
                var sourceProperty = sourceProperties.FirstOrDefault(p => string.Equals(p.Name, propertyName));
                var targetProperty = targetProperties.FirstOrDefault(p => string.Equals(p.Name, propertyName));
                if (sourceProperty == null || targetProperty == null)
                {
                    throw new InvaildEntityListPropertyException(typeof(TSource), typeof(TTarget), propertyName);
                }

                if (!sourceProperty.PropertyType.IsListOfEntityType() || !targetProperty.PropertyType.IsListOfEntityType())
                {
                    throw new InvaildEntityListPropertyException(typeof(TSource), typeof(TTarget), propertyName);
                }
            }

            KeepUnmatchedProperties = new HashSet<string>(names);
        }

        return this;
    }

    public ICustomTypeMapperConfiguration<TSource, TTarget> SetMapToDatabaseType(MapToDatabaseType mapToDatabase)
    {
        MapToDatabaseType = mapToDatabase;
        return this;
    }

    protected override void Configure(MapperBuilder configurator)
    {
        configurator.Configure<TSource, TTarget>(this);
    }
}

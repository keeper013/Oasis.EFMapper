namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Linq.Expressions;

internal interface ICustomPropertyMapperInternal
{
    IEnumerable<PropertyInfo> MappedTargetProperties { get; }

    Delegate MapProperties { get; }
}

internal class CustomPropertyMapperInternal : ICustomPropertyMapperInternal
{
    public IEnumerable<PropertyInfo> MappedTargetProperties { get; set; } = null!;

    public Delegate MapProperties { get; set; } = null!;
}

internal static class CustomPropertyInternalExtension
{
    public static ICustomPropertyMapperInternal ToInternal<TSource, TTarget>(this ICustomPropertyMapper<TSource, TTarget> intf)
        where TSource : class
        where TTarget : class
    {
        return new CustomPropertyMapperInternal
        {
            MappedTargetProperties = intf.MappedTargetProperties,
            MapProperties = intf.MapProperties,
        };
    }
}

internal class CustomPropertyMapper<TSource, TTarget> : ICustomPropertyMapper<TSource, TTarget>, ICustomPropertyMapperBuilder<TSource, TTarget>
    where TSource : class
    where TTarget : class
{
    private readonly IList<Action<TSource, TTarget>> _mappingFunctions = new List<Action<TSource, TTarget>>();
    private readonly ISet<PropertyInfo> _mappedProperties = new HashSet<PropertyInfo>();

    public IEnumerable<PropertyInfo> MappedTargetProperties => _mappedProperties.ToList();

    public ICustomPropertyMapperBuilder<TSource, TTarget> MapProperty<TProperty>(Expression<Func<TTarget, TProperty>> setter, Expression<Func<TSource, TProperty>> value)
    {
        var setterAction = CreateSetter(setter);
        var valueFunc = value.Compile();
        _mappingFunctions.Add((source, target) => setterAction(target, valueFunc(source)));
        return this;
    }

    public void MapProperties(TSource source, TTarget target)
    {
        foreach (var func in _mappingFunctions)
        {
            func(source, target);
        }
    }

    public ICustomPropertyMapper<TSource, TTarget> Build()
    {
        return this;
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

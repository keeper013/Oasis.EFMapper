namespace Oasis.EntityFramework.Mapper.InternalLogic;

using System.Linq.Expressions;

public static class ExpressionUtilities
{
    public static Expression<Func<TTarget, bool>> MakeContainsIdExpression<TTarget, TTargetId>(IList<TTargetId> list, string targetIdentityPropertyName)
        where TTarget : class
    {
        var parameter = Expression.Parameter(typeof(TTarget), "t");
        var field = Expression.Property(parameter, targetIdentityPropertyName);
        Expression<Func<IList<TTargetId>, bool>> containsExpr = (IList<TTargetId> q) => q.Contains(default!);
        var containsMethod = ((MethodCallExpression)containsExpr.Body).Method;
        var expression = Expression.Call(
            Expression.Constant(list),
            containsMethod,
            field);
        return Expression.Lambda<Func<TTarget, bool>>(expression, parameter);
    }

    public static Expression<Func<TTarget, bool>> MakeIdEqualsExpression<TTarget, TSourceId>(TSourceId sourceId, Type targetIdentityType, string identityPropertyName)
    {
        var parameter = Expression.Parameter(typeof(TTarget), "entity");

        // Expression.Convert is necessary, in case id property type is nullable.
        var equal = Expression.Equal(
                Expression.Property(parameter, identityPropertyName),
                Expression.Convert(Expression.Constant(sourceId), targetIdentityType));
        return Expression.Lambda<Func<TTarget, bool>>(equal, parameter);
    }
}

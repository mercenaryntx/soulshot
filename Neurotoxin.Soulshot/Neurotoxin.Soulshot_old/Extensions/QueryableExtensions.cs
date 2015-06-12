using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class QueryableExtensions
    {
        public static void Update<T>(this IQueryable<T> source, Expression<Action<T>> selector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");

            var provider = source.Provider;
            var currentMethod = (MethodInfo)MethodBase.GetCurrentMethod();
            var typeArray = new[] { typeof(T) };
            var methodInfo = currentMethod.MakeGenericMethod(typeArray);
            var expression = new[] { source.Expression, Expression.Quote(selector) };
            provider.Execute(Expression.Call(null, methodInfo, expression));
        }
    }

    public static class ObjectExtensions
    {
        public static void AssignNewValue<T>(this T obj, Expression<Func<T, object>> expression, object value)
        {
            ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
            Expression targetExpression = expression.Body is UnaryExpression ? ((UnaryExpression)expression.Body).Operand : expression.Body;

            //var newValue = Expression.Parameter(expression.Body.Type);
            var assign = Expression.Lambda<Action<T, object>>
                        (
                            Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                            expression.Parameters.Single(), valueParameterExpression
                        );

            assign.Compile().Invoke(obj, value);
        }
    }
}
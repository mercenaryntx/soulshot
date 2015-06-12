using System;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class QueryableExtensions
    {
        public static void Update<T>(this IQueryable<T> source, Expression selector)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");

            var provider = (SqlQueryProvider)source.Provider;
            provider.Update(source.Expression, selector);
        }

        public static void Update<T>(this IQueryable<T> source, object values)
        {
            Expression selector = null;
            foreach (var pi in values.GetType().GetProperties())
            {
                var leftExpression = new ColumnExpression(pi.Name, null, pi.PropertyType);
                var rightExpression = Expression.Constant(pi.GetValue(values), pi.PropertyType);
                Expression expression = Expression.Equal(leftExpression, rightExpression);
                selector = selector == null ? expression : new ListingExpression(selector, expression, typeof(Expression));
            }
            Update(source, selector);
        }

        //public static void Update<T>(this IQueryable<T> source, string sqlPart)
        //{
        //    if (source == null) throw new ArgumentNullException("source");
        //    if (string.IsNullOrWhiteSpace(sqlPart)) throw new ArgumentNullException("sqlPart");

        //    var provider = source.Provider;
        //    var currentMethod = (MethodInfo)MethodBase.GetCurrentMethod();
        //    var typeArray = new[] { typeof(T) }; //, typeof(T)
        //    var methodInfo = currentMethod.MakeGenericMethod(typeArray);
        //    var expression = new[] { source.Expression, new SqlPartExpression(sqlPart) };
        //    provider.Execute<T>(Expression.Call(methodInfo, expression));
        //}
    }

    //public static class ObjectExtensions
    //{
    //    public static void AssignNewValue<T>(this T obj, Expression<Func<T, object>> expression, object value)
    //    {
    //        ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
    //        Expression targetExpression = expression.Body is UnaryExpression ? ((UnaryExpression)expression.Body).Operand : expression.Body;

    //        //var newValue = Expression.Parameter(expression.Body.Type);
    //        var assign = Expression.Lambda<Action<T, object>>
    //                    (
    //                        Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
    //                        expression.Parameters.Single(), valueParameterExpression
    //                    );

    //        assign.Compile().Invoke(obj, value);
    //    }

    //    public static void AssignNewValue<T>(this T obj, object values)
    //    {
    //        Expression expression = null;
    //        foreach (var pi in values.GetType().GetProperties())
    //        {
    //            var valueParameterExpression = Expression.Parameter(typeof(object));
    //            var targetExpression = Expression.Property(Expression.Constant(values), pi);

    //            var assign = Expression.Lambda<Action<T, object>>
    //                        (
    //                            Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
    //                            Expression.Parameter(targetExpression.Type, "o"), valueParameterExpression
    //                        );

    //            assign.Compile().Invoke(obj, pi.GetValue(values));
    //        }
    //        return expression;
    //    }
    //}
}
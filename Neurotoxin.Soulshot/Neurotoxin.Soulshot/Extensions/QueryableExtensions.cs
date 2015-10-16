using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
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

        public static IQueryable<T> OfType<T>(this IQueryable<T> source, Type entityType)
        {
            if (source == null) throw new ArgumentNullException("source");
            var provider = source.Provider;
            var methodInfo = ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new [] { typeof(T) });
            var expressions = new[] { source.Expression, Expression.Constant(entityType) };
            return provider.CreateQuery<T>(Expression.Call(null, methodInfo, expressions));
        }

        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, Func<T, bool> selector)
        {
            return Task.Run(() => source.FirstOrDefault(selector));
        }

        public static Task<IQueryable<T>> ReadAsync<T>(this IQueryable<T> source)
        {
            //return Task.Run(() => source.)
            throw new NotImplementedException();
        }

        public static IQueryable<T> Include<T>(this IQueryable<T> source, string path)
        {
            throw new NotImplementedException();
        }
    }
}
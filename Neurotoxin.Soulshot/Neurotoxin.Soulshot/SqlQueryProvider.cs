using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class SqlQueryProvider : IQueryProvider
    {
        private readonly IDataEngine _dataEngine;

        public SqlQueryProvider(IDataEngine dataEngine)
        {
            _dataEngine = dataEngine;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Table<>).MakeGenericType(elementType), this, expression);
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return new Table<TResult>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var methodCallExpression = expression as MethodCallExpression;
            var methodName = methodCallExpression?.Method.Name;

            var elementType = TypeSystem.GetElementType(expression.Type);
            if (methodName == MethodNames.Any) elementType = typeof(int);
            var list = ExecuteImp(expression, elementType);
            var enumerator = list.GetEnumerator();
            switch (methodName)
            {
                case MethodNames.First:
                case MethodNames.FirstOrDefault:
                case MethodNames.Single:
                case MethodNames.SingleOrDefault:
                    if (enumerator.MoveNext())
                    {
                        var first = (TResult) enumerator.Current;
                        if (methodName == MethodNames.Single && enumerator.MoveNext())
                        {
                            throw new InvalidOperationException("The input sequence contains more than one element");
                        }
                        return first;
                    }
                    if (methodName == MethodNames.FirstOrDefault || 
                        methodName == MethodNames.SingleOrDefault) return default(TResult);
                    throw new InvalidOperationException("Sequence contains no elements");
                case MethodNames.Max:
                    enumerator.MoveNext();
                    return (TResult)enumerator.Current;
                case MethodNames.Count:
                    enumerator.MoveNext();
                    return (TResult)enumerator.Current;
                case MethodNames.Any:
                    enumerator.MoveNext();
                    var value = (object)((int)enumerator.Current != 0);
                    return (TResult)value;
                default:
                    return (TResult)list;
            }
        }

        private IEnumerable ExecuteImp(Expression expression, Type elementType)
        {
            return _dataEngine.ExecuteQuery(expression, elementType);
        }
    }
}
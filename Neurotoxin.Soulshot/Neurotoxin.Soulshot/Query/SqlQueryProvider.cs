using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class SqlQueryProvider : IQueryProvider
    {
        public IDataEngine DataEngine { get; private set; }
        public IDbSet DbSet { get; private set; }

        public SqlQueryProvider(IDataEngine dataEngine, IDbSet dbSet)
        {
            DataEngine = dataEngine;
            DbSet = dbSet;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            //var elementType = TypeSystem.GetElementType(expression.Type);
            //try
            //{
            //    return (IQueryable)Activator.CreateInstance(typeof(DbSet<>).MakeGenericType(elementType), new object[] { this, expression });
            //}
            //catch (System.Reflection.TargetInvocationException tie)
            //{
            //    throw tie.InnerException;
            //}
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var type = typeof(TElement);
            var mapper = DataEngine.ColumnMapper.TryGetMapper(type);
            if (mapper != null)
            {
                var list = (List<TElement>)Select(expression, TypeSystem.GetElementType(expression.Type));
                return list.AsQueryable();
            }

            //TODO: return the same dbset perhaps?
            return new DbSet<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
            return ExecuteImp(expression, null);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            if (expression is UpdateExpression)
            {
                
            }

            var type = typeof(TResult);
            //var mapper = DataEngine.ColumnMapper.TryGetMapper(type);
            //if (mapper != null)
            //{
            //    var sqlExpression = ExecuteImp(expression, typeof(SelectExpression));
            //    var scalar = DataEngine.ExecuteScalar(sqlExpression, type);
            //    return (TResult)scalar;
            //}

            var methodCallExpression = expression as MethodCallExpression;
            if (methodCallExpression == null) throw new InvalidOperationException();
            var methodName = methodCallExpression.Method.Name;
            
            var elementType = TypeSystem.GetElementType(expression.Type);
            if (methodName == "Any") elementType = typeof (int);
            var list = Select(expression, elementType);
            if (type == elementType)
            {
                var enumerator = list.GetEnumerator();
                TResult result;
                switch (methodName)
                {
                    case "FirstOrDefault":
                        result = First<TResult>(enumerator, true);
                        DbSet.CacheEntity(result);
                        return result;
                    case "First":
                        result = First<TResult>(enumerator, false);
                        DbSet.CacheEntity(result);
                        return result;
                    case "SingleOrDefault":
                        result = Single<TResult>(enumerator, true);
                        DbSet.CacheEntity(result);
                        return result;
                    case "Single":
                        result = Single<TResult>(enumerator, false);
                        DbSet.CacheEntity(result);
                        return result;
                    case "Max":
                        enumerator.MoveNext();
                        return (TResult)enumerator.Current;
                    case "Count":
                        enumerator.MoveNext();
                        return (TResult)enumerator.Current;
                    default:
                        throw new NotSupportedException(methodCallExpression.Method.Name);
                }
            }
            if (type.IsValueType)
            {
                var enumerator = list.GetEnumerator();
                switch (methodName)
                {
                    case "Count":
                        enumerator.MoveNext();
                        return (TResult)enumerator.Current;
                    case "Any":
                        enumerator.MoveNext();
                        var value = (object)((int)enumerator.Current != 0);
                        return (TResult)value;
                    default:
                        throw new NotSupportedException(methodCallExpression.Method.Name);
                }
            }
            DbSet.CacheEntities(list);
            return (TResult)list;
        }

        public List<T> Select<T>(Expression expression)
        {
            return (List<T>)Select(expression, TypeSystem.GetElementType(expression.Type));
        }

        private IEnumerable Select(Expression expression, Type elementType)
        {
            var sqlExpression = ExecuteImp(expression, typeof(SelectExpression));
            return DataEngine.ExecuteQueryExpression(elementType, sqlExpression);
        }

        public void Delete(Expression expression)
        {
            var sqlExpression = ExecuteImp(expression, typeof(DeleteExpression));
            DataEngine.ExecuteNonQueryExpression(sqlExpression);
        }

        public void Update(Expression expression, Expression updateExpression)
        {
            var sqlExpression = (UpdateExpression)ExecuteImp(expression, typeof(UpdateExpression));
            sqlExpression.Set = updateExpression;
            DataEngine.ExecuteNonQueryExpression(sqlExpression);
        }

        private SqlExpression ExecuteImp(Expression expression, Type targetExpression)
        {
            var sqlVisitor = new LinqToSqlVisitor(DbSet, targetExpression);
            sqlVisitor.Visit(expression);
            return sqlVisitor.GetResult();
        }

        private T First<T>(IEnumerator enumerator, bool orDefault)
        {
            if (enumerator.MoveNext()) return (T)enumerator.Current;
            if (orDefault) return default(T);
            throw new InvalidOperationException("Sequence contains no elements");
        }

        private T Single<T>(IEnumerator enumerator, bool orDefault)
        {
            var first = First<T>(enumerator, orDefault);
            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException("The input sequence contains more than one element");
            }
            else
            {
                return first;
            }
        }
    }
}
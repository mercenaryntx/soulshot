﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Query
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
                var list = (List<TElement>) Select(expression);
                return list.AsQueryable();
            }

            return new DbSet<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
            return ExecuteImp(expression, null);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var type = typeof(TResult);
            var mapper = DataEngine.ColumnMapper.TryGetMapper(type);
            if (mapper != null)
            {
                var sqlExpression = ExecuteImp(expression, typeof(SelectExpression));
                var scalar = DataEngine.ExecuteScalar(sqlExpression, type);
                return (TResult)scalar;
            }
            
            var elementType = TypeSystem.GetElementType(expression.Type);
            var list = Select(expression);
            if (typeof (TResult) == elementType)
            {
                var enumerator = list.GetEnumerator();
                enumerator.MoveNext();
                return (TResult)enumerator.Current;
            }
            return (TResult)list;
        }

        public List<T> Select<T>(Expression expression)
        {
            return (List<T>)Select(expression);
        }

        private IEnumerable Select(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            var sqlExpression = ExecuteImp(expression, typeof(SelectExpression));
            return DataEngine.ExecuteQuery(elementType, sqlExpression);
        }

        public void Delete(Expression expression)
        {
            var sqlExpression = ExecuteImp(expression, typeof(DeleteExpression));
            DataEngine.ExecuteNonQuery(sqlExpression);
        }

        private SqlExpression ExecuteImp(Expression expression, Type targetExpression)
        {
            var sqlVisitor = new LinqToSqlVisitor(DbSet, targetExpression);
            sqlVisitor.Visit(expression);
            return sqlVisitor.GetResult();
        }
    }
}
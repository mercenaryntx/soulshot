using System;
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
        public TableAttribute Table { get; private set; }
        public List<ColumnInfo> Columns { get; private set; }

        public SqlQueryProvider(IDataEngine dataEngine, TableAttribute table, List<ColumnInfo> columns)
        {
            DataEngine = dataEngine;
            Table = table;
            Columns = columns;
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
            return new DbSet<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            throw new NotImplementedException();
            return ExecuteImp(expression, null);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotImplementedException();
            //return (TResult)ExecuteImp(expression, typeof(TResult));
            ////TODO: (typeof(TResult).Name == "IEnumerable`1");
            //var isEnumerable = false;
            //return (TResult)SqlQueryContext.Execute(expression, isEnumerable);
        }

        public List<T> Select<T>(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            var sqlExpression = ExecuteImp(expression, typeof (SelectExpression));
            return (List<T>)DataEngine.Execute(elementType, sqlExpression);
        }

        public void Delete(Expression expression)
        {
            var sqlExpression = ExecuteImp(expression, typeof(DeleteExpression));
            DataEngine.ExecuteNonQuery(sqlExpression);
        }

        private SqlExpression ExecuteImp(Expression expression, Type targetExpression)
        {
            var sqlVisitor = new LinqToSqlVisitor(Table, Columns, targetExpression);
            sqlVisitor.Visit(expression);
            return sqlVisitor.GetResult();
        }
    }
}
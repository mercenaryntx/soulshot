using System;
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
            var elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(DbSet<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new DbSet<TElement>(this, expression);
        }

        public object Execute(Expression expression)
        {
            return ExecuteImp(expression, null);
            //return SqlQueryContext.Execute(expression, false);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)ExecuteImp(expression, typeof(TResult));
            ////TODO: (typeof(TResult).Name == "IEnumerable`1");
            //var isEnumerable = false;
            //return (TResult)SqlQueryContext.Execute(expression, isEnumerable);
        }

        private object ExecuteImp(Expression expression, Type targetType)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            var sqlVisitor = new LinqToSqlVisitor(Table, Columns);
            sqlVisitor.Visit(expression);
            //TODO: enumarable check
            //TODO: provide DbSet
            //TODO: move the string builder part within the engine
            return DataEngine.Execute(elementType, sqlVisitor.GetResult());
        }
    }
}

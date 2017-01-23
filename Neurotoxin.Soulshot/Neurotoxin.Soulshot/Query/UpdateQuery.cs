using System;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot.Query
{
    public class UpdateQuery<TEntity> : UpdateQuery
    {
        public UpdateQuery(TableHint tableHint = TableHint.None) : base(typeof(TEntity), tableHint)
        {
        }
    }

    public class UpdateQuery : LinqToSqlVisitor<UpdateExpression>
    {
        public ColumnMapping[] Columns { get; private set; }

        public UpdateQuery(Type entityType, TableHint tableHint = TableHint.None) : base(entityType, tableHint)
        {
            Update = new UpdateExpression();
        }

        protected override SqlExpression GetSqlExpressionInner()
        {
            if (Update.Target == null) Update.Target = From;
            Update.AddWhere(Where);
            return Update;
        }

        public static UpdateQuery<TEntity> CreateParameterizedQuery<TEntity>(bool isMemoryOptimizedTable, string[] properties = null) where TEntity : class
        {
            var query = new UpdateQuery<TEntity>(isMemoryOptimizedTable ? TableHint.Snapshot : TableHint.None);
            var from = query.From;
            query.Update.Target = from;
            query.Columns = query.GetColumnMappings().Columns
                                 .Where(a => properties == null || a.IsPrimaryKey || properties.Contains(a.Property.Name))
                                 .Select((a, i) => a.Clone(i)).ToArray();
            foreach (var a in query.Columns.Where(a => a.IsPrimaryKey))
            {
                var columnExpression = new ColumnExpression(a.ColumnName, from, a.Property.PropertyType);
                var expression = Expression.MakeBinary(ExpressionType.Equal, columnExpression, new ParameterExpression(a.Index, a.Property.PropertyType));
                query.AddWhere(expression);
            }
            foreach (var a in query.Columns.Where(a => !a.IsPrimaryKey))
            {
                var columnExpression = new ColumnExpression(a.ColumnName, from, a.Property.PropertyType);
                var expression = Expression.MakeBinary(ExpressionType.Equal, columnExpression, new ParameterExpression(a.Index, a.Property.PropertyType));
                query.Update.AddSet(expression);
            }
            if (query.Where == null) throw new ArgumentException($"Primary key definition is missing from entity type {typeof(TEntity).FullName}");
            return query;
        }
    }
}
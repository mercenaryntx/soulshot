using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EPAM.ReserveAIR.Shared.Repositories;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;
using PetaPoco;

namespace Neurotoxin.Soulshot
{
    public class Table<TEntity> : IOrderedQueryable<TEntity>, ITable
    {
        public IDataEngine DataEngine { get; }
        private SqlQueryProvider Provider { get; }
        IQueryProvider IQueryable.Provider => Provider;
        public Expression Expression { get; }
        public Type ElementType { get; }
        public TableAttribute TableDefinition { get; }

        public Table(IDataEngine dataEngine)
        {
            var type = typeof(TEntity);
            DataEngine = dataEngine;
            Provider = new SqlQueryProvider(DataEngine);
            Expression = Expression.Constant(this);
            ElementType = type;
            TableDefinition = type.GetCustomAttribute<TableAttribute>() ?? new TableAttribute(type.Name);
        }

        internal Table(SqlQueryProvider provider, Expression expression)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type)) throw new ArgumentOutOfRangeException(nameof(expression));

            Provider = provider;
            Expression = expression;
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<TEntity>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        public void Truncate()
        {
            DataEngine.Truncate(TableDefinition.FullNameWithBrackets);
        }

        public void Delete(Expression<Func<TEntity, bool>> expression)
        {
            var query = new DeleteQuery<TEntity>();
            var sqlCommand = query.ToSqlString(expression);
            DataEngine.ExecuteCommand(sqlCommand);
        }

        public void Delete(Expression expression)
        {
            var query = new DeleteQuery<TEntity>();
            var sqlCommand = query.ToSqlString(expression);
            DataEngine.ExecuteCommand(sqlCommand);
        }

        public void BulkInsert(IEnumerable<TEntity> entities, BulkInsertOptions options, Action<TEntity> beforeItemPersist = null, Action<int> afterPagePersist = null)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (options == null) throw new ArgumentException(nameof(options));

            var tableName = TableDefinition.FullNameWithBrackets;

            if (options.DisableIndexes) DataEngine.AlterIndexes(tableName, "DISABLE");
            DataEngine.BulkInsert(tableName, entities, options, beforeItemPersist, afterPagePersist);
            if (options.DisableIndexes) DataEngine.AlterIndexes(tableName, "REBUILD");
        }

        //public bool IsMemoryOptimized<TEntity>() where TEntity : class
        //{
        //    var entityType = typeof(TEntity);
        //    var tableName = table.Context.Mapping.GetTable(entityType).TableName;
        //    var isMemoryOptimizedQuery = $@"SELECT is_memory_optimized FROM sys.tables t
        //                                    WHERE t.object_id = OBJECT_ID('{tableName}')";
        //    var isMemoryOptimized = table.Context.ExecuteQuery<bool?>(isMemoryOptimizedQuery).FirstOrDefault();
        //    if (!isMemoryOptimized.HasValue) throw new ArgumentException($"Table {tableName} cannot be found");
        //    return isMemoryOptimized.Value;
        //}

        //public void Update<TEntity>(, IEnumerable<TEntity> entities, bool parameterizedCommand, bool isMemoryOptimizedTable, ChangeTracker<TEntity> changeTracker, Action<TEntity> beforeAction = null) where TEntity : class, INotifyPropertyChanged
        //{
        //    Update(table, entities, parameterizedCommand, isMemoryOptimizedTable, changeTracker?.ChangedColumns, beforeAction);
        //}

        //public void Update<TEntity>(, IEnumerable<TEntity> entities, bool parameterizedCommand, bool isMemoryOptimizedTable, string[] properties = null, Action<TEntity> beforeAction = null) where TEntity : class
        //{
        //    if (entities == null) throw new ArgumentNullException(nameof(entities));

        //    var query = UpdateQuery.CreateParameterizedQuery<TEntity>(isMemoryOptimizedTable, properties);
        //    var queryMode = parameterizedCommand
        //        ? ParameterizedQueryMode.ParameterizedQuery
        //        : ParameterizedQueryMode.StringFormat;
        //    var commandText = query.ToSqlString(null, queryMode);

        //    using (var transaction = table.Context.Connection.BeginTransaction())
        //    {
        //        try
        //        {
        //            table.Context.Transaction = transaction;

        //            if (parameterizedCommand)
        //            {
        //                foreach (var entity in entities)
        //                {
        //                    var cmd = table.Context.Connection.CreateCommand();
        //                    cmd.Transaction = transaction;
        //                    cmd.CommandText = commandText;
        //                    cmd.Parameters.AddRange(query.Columns.Select(cm =>
        //                    {
        //                        var p = cmd.CreateParameter();
        //                        p.ParameterName = $"p{cm.Index}";
        //                        p.Value = cm.Property.GetValue(entity, null) ?? DBNull.Value;
        //                        return p;
        //                    }).ToArray());
        //                    cmd.ExecuteNonQuery();
        //                }
        //            }
        //            else
        //            {
        //                var sb = new StringBuilder();
        //                var i = 0;
        //                foreach (var entity in entities)
        //                {
        //                    beforeAction?.Invoke(entity);
        //                    var args = query.Columns.Select(cm => SqlCommandTextVisitor.GetLiteral(cm.Property.GetValue(entity, null))).Cast<object>().ToArray();
        //                    sb.AppendLine(string.Format(commandText, args));
        //                    i++;
        //                    if (i % 5000 == 0)
        //                    {
        //                        table.Context.ExecuteCommand(sb.ToString());
        //                        sb.Clear();
        //                    }
        //                }
        //                table.Context.ExecuteCommand(sb.ToString());
        //            }
        //            transaction.Commit();
        //        }
        //        catch (Exception)
        //        {
        //            transaction.Rollback();
        //            throw;
        //        }
        //        table.Context.Transaction = null;
        //    }
        //}

    }
}
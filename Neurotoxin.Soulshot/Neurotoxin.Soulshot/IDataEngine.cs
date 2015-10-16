using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot
{
    public interface IDataEngine : IDisposable
    {
        ColumnMapper ColumnMapper { get; set; }

        bool TableExists<TEntity>();
        bool TableExists(TableAttribute table);
        IColumnInfoCollection UpdateTable<TEntity>(TableAttribute table, IColumnInfoCollection actualColumns, IEnumerable<ColumnInfo> storedColumns);
        void RenameTable(TableAttribute oldName, TableAttribute newName);
        IEnumerable<TEntity> Execute<TEntity>(Expression expression);
        void CommitChanges(IEnumerable entities, TableAttribute table, IColumnInfoCollection columns);
        void BulkInsert(IEnumerable entities, TableAttribute table, IColumnInfoCollection columns);
        string GetLiteral(object value);

        void ExecuteNonQueryExpression(Expression expression);
        IEnumerable ExecuteQueryExpression(Type elementType, Expression expression);
        object ExecuteScalarExpression(Expression expression, Type type);

        IEnumerable<T> ExecuteQuery<T>(string command, params SqlParameter[] args);
    }
}
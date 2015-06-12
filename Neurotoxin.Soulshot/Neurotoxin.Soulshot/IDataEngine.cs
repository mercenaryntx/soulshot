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
        ColumnInfoCollection UpdateTable<TEntity>(TableAttribute table, ColumnInfoCollection actualColumns, ColumnInfoCollection storedColumns);
        void RenameTable(TableAttribute oldName, TableAttribute newName);
        IEnumerable<TEntity> Execute<TEntity>(Expression expression);
        void CommitChanges(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns);
        void BulkInsert(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns);
        string GetLiteral(object value);

        void ExecuteNonQueryExpression(Expression expression);
        IEnumerable ExecuteQueryExpression(Type elementType, Expression expression);
        object ExecuteScalarExpression(Expression expression, Type type);
    }
}
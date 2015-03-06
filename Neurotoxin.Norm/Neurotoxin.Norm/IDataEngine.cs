using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm
{
    public interface IDataEngine : IDisposable
    {
        ColumnMapper ColumnMapper { get; set; }

        bool TableExists<TEntity>();
        bool TableExists(TableAttribute table);
        ColumnInfoCollection UpdateTable<TEntity>(TableAttribute table, ColumnInfoCollection actualColumns, ColumnInfoCollection storedColumns);
        void RenameTable(TableAttribute oldName, TableAttribute newName);
        void ExecuteNonQuery(Expression expression);
        IEnumerable ExecuteQuery(Type elementType, Expression expression);
        IEnumerable<TEntity> Execute<TEntity>(Expression expression);
        void CommitChanges(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns);
        string GetLiteral(object value);
        object ExecuteScalar(Expression expression, Type type);
    }
}
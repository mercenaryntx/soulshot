using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    public interface IDataEngine : IDisposable
    {
        bool TableExists<TEntity>();
        bool TableExists(TableAttribute table);
        List<ColumnInfo> CreateTable<TEntity>();
        List<ColumnInfo> CreateTable<TEntity>(TableAttribute table);
        List<ColumnInfo> UpdateTable<TEntity>(TableAttribute table, List<ColumnInfo> storedColumns);
        void RenameTable(TableAttribute oldName, TableAttribute newName);
        void ExecuteNonQuery(Expression expression);
        IEnumerable Execute(Type elementType, Expression expression);
        IEnumerable<TEntity> Execute<TEntity>(Expression expression);
        void CommitChanges(IEnumerable entities, TableAttribute table, IEnumerable<ColumnInfo> columns);
        string GetLiteral(object value);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using EPAM.ReserveAIR.Shared.Repositories;

namespace Neurotoxin.Soulshot
{
    public interface IDataEngine : IDisposable
    {
        IDbConnection Connection { get; }
        string ConnectionString { get; set; }

        IEnumerable ExecuteQuery(Expression expression, Type entityType);
        IEnumerable ExecuteQuery(string query, Type entityType);
        IEnumerable<T> ExecuteQuery<T>(string query);
        void ExecuteCommand(string command);

        void Truncate(string tableName);
        void BulkInsert<T>(string tableName, IEnumerable<T> entities, BulkInsertOptions options, Action<T> beforeItemPersist = null, Action<int> afterPagePersist = null);
        void AlterIndexes(string tableName, string option);
    }
}
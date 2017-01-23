using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using EPAM.ReserveAIR.Shared.Repositories;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using Neurotoxin.Soulshot.Query;
using PetaPoco;

namespace Neurotoxin.Soulshot
{
    public class SqlServerDataEngine : IDataEngine
    {
        //TODO: config
        private readonly IMapper _defaultMapper = new ConventionMapper();
        public IDbConnection Connection { get; }
        public string ConnectionString { get; set; }

        public SqlServerDataEngine(IDbConnection connection)
        {
            Connection = connection;
            ConnectionString = connection.ConnectionString;
        }

        public SqlServerDataEngine(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public IEnumerable ExecuteQuery(Expression expression, Type entityType)
        {
            var s = new SelectQuery(entityType);
            var query = s.ToSqlString(expression);
            return ExecuteQuery(query, entityType);
        }

        public IEnumerable ExecuteQuery(string query, Type entityType)
        {
            var listType = typeof(List<>).MakeGenericType(entityType);
            var addMethod = listType.GetMethod("Add");
            var list = (IEnumerable)Activator.CreateInstance(listType);

            var connection = Connection as SqlConnection ?? new SqlConnection(ConnectionString);
            try
            {
                if (connection.State == ConnectionState.Closed) connection.Open();
                using (var cmd = new SqlCommand(query, connection))
                {
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var factory = GetFactory(entityType, reader, query);
                        var instance = factory.DynamicInvoke(reader);
                        //yield return instance;
                        addMethod.Invoke(list, new[] { instance });
                    }
                    reader.Close();
                }
            }
            finally
            {
                if (Connection == null) connection.Close();
            }
            return list;
        }

        public IEnumerable<T> ExecuteQuery<T>(string query)
        {
            return (IEnumerable<T>)ExecuteQuery(query, typeof(T));
        }

        public void ExecuteCommand(string command)
        {
            var connection = Connection as SqlConnection ?? new SqlConnection(ConnectionString);
            try
            {
                if (connection.State == ConnectionState.Closed) connection.Open();
                using (var cmd = new SqlCommand(command, connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            finally
            {
                if (Connection == null) connection.Close();
            }
        }

        public void Truncate(string tableName)
        {
            ExecuteCommand($"TRUNCATE TABLE {tableName}");
        }

        public void BulkInsert<T>(string tableName, IEnumerable<T> entities, BulkInsertOptions options, Action<T> beforeItemPersist = null, Action<int> afterPagePersist = null)
        {
            var entityType = typeof (T);
            var connection = Connection as SqlConnection ?? new SqlConnection(ConnectionString);
            try
            {
                using (var bulkCopy = new SqlBulkCopy(connection, options.CopyOptions, null)
                {
                    DestinationTableName = tableName
                })
                {
                    bulkCopy.BulkCopyTimeout = options.Timeout;
                    var mapping = entityType.GetColumnMappings();
                    var dataTable = new DataTable();

                    foreach (var m in mapping.Columns)
                    {
                        dataTable.Columns.Add(new DataColumn(m.ColumnName, m.Type));
                    }

                    var i = 0;
                    foreach (var entity in entities)
                    {
                        beforeItemPersist?.Invoke(entity);
                        dataTable.Rows.Add(mapping.Columns.Select(c => c.GetValue(entity) ?? DBNull.Value).ToArray());
                        if (i == options.PageSize)
                        {
                            bulkCopy.WriteToServer(dataTable);
                            afterPagePersist?.Invoke(i);
                            dataTable.Clear();
                            i = 0;
                        }
                        else
                        {
                            i++;
                        }
                    }
                    bulkCopy.WriteToServer(dataTable);
                    afterPagePersist?.Invoke(i);
                }
            }
            finally
            {
                if (Connection == null) connection.Close();
            }
        }

        public void AlterIndexes(string tableName, string option)
        {
            var getIndexes = $@"SELECT i.name indexName 
                                FROM sys.indexes i
                                JOIN sys.objects o ON i.object_id = o.object_id 
                                WHERE i.type_desc = 'NONCLUSTERED' 
                                AND o.type_desc = 'USER_TABLE'
                                AND o.object_id = OBJECT_ID('{tableName}')";
            var indexes = ExecuteQuery<string>(getIndexes).ToArray();
            if (indexes.Length == 0) return;

            var sb = new StringBuilder();
            foreach (var index in indexes)
            {
                sb.AppendLine($"ALTER INDEX [{index}] ON {tableName} {option}");
            }
            ExecuteCommand(sb.ToString());
        }

        private Delegate GetFactory(Type type, IDataReader reader, string query)
        {
            var mapping = type.GetColumnMappings();
            if (mapping.TableDefinition?.MappingStrategy == MappingStrategy.TablePerHierarchy)
            {
                if (reader.GetName(0) != ColumnMapping.DiscriminatorColumnName) throw new Exception("Invalid column");
                //TODO: tph detection, discriminator literal, type find
                type = type.Assembly.GetType(reader.GetString(0));
            }
            var pd = PocoData.ForType(type, _defaultMapper);
            return pd.GetFactory(query, ConnectionString, 0, reader.FieldCount, reader, _defaultMapper);
        }

        public void Dispose()
        {
            
        }
    }
}
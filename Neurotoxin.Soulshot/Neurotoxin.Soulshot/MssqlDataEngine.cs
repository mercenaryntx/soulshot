using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class MssqlDataEngine : DataEngineBase
    {
        private string _database;
        private SqlConnection _connection;
        private SqlTransaction _transaction;

        public string ConnectionString { get; private set; }

        public MssqlDataEngine(string connectionString)
        {
            var r = new Regex("Initial Catalog=(.*?);", RegexOptions.IgnoreCase);
            var m = r.Match(connectionString);
            string database;
            if (m.Success)
            {
                database = m.Groups[1].Value;
                ConnectionString = r.Replace(connectionString, string.Empty);
            }
            else
            {
                throw new ArgumentException("Initial Catalog is missing");
            }

            EnsureDatabase(database);
        }

        private void EnsureDatabase(string database)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    var count = ExecuteScalar<int>(string.Format("SELECT count(*) FROM master.dbo.sysdatabases WHERE name = '{0}'", database));
                    if (count == 0) ExecuteNonQuery(string.Format("CREATE DATABASE {0}", database));
                    _database = database;
                }
            }
        }

        protected override IEnumerable<ConstraintInfo> GetForeignReferences(TableAttribute table)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    var cmd = string.Format(@"SELECT p.name as TableName, ps.name as TableSchema, fk.name as ConstraintName, cc.name as TargetColumn, pc.name as SourceColumn
                                    FROM sys.foreign_key_columns AS r
                                    INNER JOIN sys.tables AS c ON c.object_id = r.referenced_object_id
                                    INNER JOIN sys.schemas AS cs ON cs.schema_id = c.schema_id
                                    INNER JOIN sys.objects AS fk ON fk.object_id = r.constraint_object_id
                                    INNER JOIN sys.tables AS p ON p.object_id = r.parent_object_id
                                    INNER JOIN sys.schemas AS ps ON ps.schema_id = p.schema_id
                                    INNER JOIN sys.columns AS cc ON cc.column_id = r.referenced_column_id AND cc.object_id = c.object_id
                                    INNER JOIN sys.columns AS pc ON pc.column_id = r.parent_column_id AND pc.object_id = p.object_id
                                    WHERE c.name = '{0}' and cs.name = '{1}'", table.Name, table.Schema);
                    if (!ColumnMapper.ContainsKey(typeof (ConstraintInfo)))
                    {
                        ColumnMapper.Map<ConstraintInfo>();
                    }
                    return ExecuteQuery<ConstraintInfo>(cmd);
                }
            }
        }

        public override bool TableExists(TableAttribute table)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    var cmd = string.Format(@"select count(*) from sys.objects o
									  inner join sys.schemas s on s.schema_id = o.schema_id
									  where type = 'U' and o.name = '{0}' and s.name = '{1}'", table.Name, table.Schema);
                    var count = ExecuteScalar<int>(cmd);
                    return count == 1;
                }
            }
        }

        public override void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns, bool generateConstraints = true)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    using (_transaction = _connection.BeginTransaction())
                    {
                        try
                        {
                            base.CreateTable(table, columns, generateConstraints);
                            _transaction.Commit();
                        }
                        catch (Exception)
                        {
                            _transaction.Rollback();
                            throw;
                        }
                    }
                    _transaction = null;
                }
            }
        }

        public override void RenameTable(TableAttribute oldName, TableAttribute newName)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    var cmd = _connection.CreateCommand();
                    cmd.CommandText = string.Format("EXEC sp_rename '{0}', '{1}'", oldName.FullName, newName.Name);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override void DeleteTable(TableAttribute table)
        {
            var drop = new DropTableExpression(new TableExpression(table));
            ExecuteNonQuery(drop);
        }

        public override void CommitChanges(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    using (_transaction = _connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (IEntityProxy entity in entities)
                            {
                                switch (entity.State)
                                {
                                    case EntityState.Added:
                                        Insert(entity, table, columns);
                                        var pk = columns.SingleOrDefault(c => c.IsIdentity);
                                        if (pk != null)
                                        {
                                            var idCommand = string.Format("SELECT CAST(@@IDENTITY AS {0})",
                                                pk.ColumnType);
                                            pk.SetValue(entity, ExecuteScalar(idCommand));
                                        }
                                        entity.ClearDirty();
                                        entity.State = EntityState.Unchanged;
                                        break;
                                    case EntityState.Changed:
                                        Update(entity, table, columns);
                                        entity.ClearDirty();
                                        entity.State = EntityState.Unchanged;
                                        break;
                                    case EntityState.Deleted:
                                        Delete(entity, table, columns);
                                        break;
                                }
                            }
                            _transaction.Commit();
                        }
                        catch (Exception)
                        {
                            _transaction.Rollback();
                            throw;
                        }
                    }
                    _transaction = null;
                }
            }
        }

        public override void BulkInsert(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    var sb = new StringBuilder();
                    foreach (var entity in entities)
                    {
                        var expression = CreateInsertExpression(entity, table, columns);
                        var visitor = new SqlCommandTextVisitor(this);
                        visitor.Visit(expression);
                        sb.AppendLine(visitor.CommandText);
                    }
                    ExecuteNonQuery(sb.ToString());
                }
            }
        }

        public override string GetLiteral(object value)
        {
            if (value == null) return "null";
            return ColumnMapper.MapToSql(value);
        }

        public override void ExecuteNonQueryExpression(Expression expression)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    ExecuteNonQuery(expression);
                }
            }
        }

        public override IEnumerable ExecuteQueryExpression(Type elementType, Expression expression)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    return ExecuteQuery(elementType, expression);
                }
            }
        }

        public override object ExecuteScalarExpression(Expression expression, Type type)
        {
            using (_connection = OpenConnection())
            {
                lock (_connection)
                {
                    _transaction = null;
                    return ExecuteScalar(expression, type);
                }
            }
        }

        protected override IEnumerable ExecuteQuery(Type elementType, Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return ExecuteQuery(elementType, visitor.CommandText);
        }

        protected override void ExecuteNonQuery(Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            ExecuteNonQuery(visitor.CommandText);
        }

        protected override object ExecuteScalar(Expression expression, Type type)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return ExecuteScalar(visitor.CommandText);
        }

        private IEnumerable<T> ExecuteQuery<T>(string command)
        {
            return (IEnumerable<T>) ExecuteQuery(typeof(T), command);
        }

        private IEnumerable ExecuteQuery(Type type, string command)
        {
            Debug.WriteLine(command);

            var columns = ColumnMapper.ContainsKey(type) ? ColumnMapper[type] : null;
            var listType = typeof(List<>).MakeGenericType(type);
            var addMethod = listType.GetMethod("Add");
            var list = (IEnumerable)Activator.CreateInstance(listType);
            try
            {
                using (var cmd = new SqlCommand(command, _connection))
                {
                    cmd.Transaction = _transaction;
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        var dict = new Dictionary<string, object>();
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            dict.Add(reader.GetName(i), reader.GetValue(i));
                        }

                        var instance = MapType(type, dict, columns);
                        addMethod.Invoke(list, new[] {instance});
                    }
                    reader.Close();
                }
                return list;
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw new Exception("SQL command failed: " + command, ex);
            }
        }

        private void ExecuteNonQuery(string command)
        {
            Debug.WriteLine(command);

            try
            {
                using (var cmd = new SqlCommand(command, _connection))
                {
                    cmd.Transaction = _transaction;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SQL command failed: " + command, ex);
            }
        }

        private T ExecuteScalar<T>(string command)
        {
            return (T)ExecuteScalar(command);
        }

        private object ExecuteScalar(string command)
        {
            Debug.WriteLine(command);

            try
            {
                using (var cmd = new SqlCommand(command, _connection))
                {
                    cmd.Transaction = _transaction;
                    var value = cmd.ExecuteScalar();
                    return ColumnMapper.MapToType(value);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("SQL command failed: " + command, ex);
            }
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if (_database != null) connection.ChangeDatabase(_database);
            return connection;
        }

        public override void Dispose()
        {
        }
    }
}
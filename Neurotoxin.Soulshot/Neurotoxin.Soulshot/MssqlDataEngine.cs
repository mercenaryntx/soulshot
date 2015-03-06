using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class MssqlDataEngine : DataEngineBase
    {
        private readonly SqlConnection _connection;
        private SqlTransaction _transaction;

        public string ConnectionString
        {
            get { return _connection.ConnectionString; }
        }

        public MssqlDataEngine(string connectionString)
        {
            var r = new Regex("Initial Catalog=(.*?);", RegexOptions.IgnoreCase);
            var m = r.Match(connectionString);
            string database;
            if (m.Success)
            {
                database = m.Groups[1].Value;
                connectionString = r.Replace(connectionString, string.Empty);
            }
            else
            {
                throw new ArgumentException("Initial Catalog is missing");
            }

            _connection = new SqlConnection(connectionString);
            _connection.Open();
            EnsureDatabase(database);
        }

        private void EnsureDatabase(string database)
        {
            var count = ExecuteScalar<int>(string.Format("SELECT count(*) FROM master.dbo.sysdatabases WHERE name = '{0}'", database));
            if (count == 0)
            {
                ExecuteNonQuery(string.Format("CREATE DATABASE {0}", database));
            }
            _connection.ChangeDatabase(database);
        }

        public override bool TableExists(TableAttribute table)
        {
            var cmd = string.Format(@"select count(*) from sys.objects o
									  inner join sys.schemas s on s.schema_id = o.schema_id
									  where type = 'U' and o.name = '{0}' and s.name = '{1}'", table.Name, table.Schema);
            var count = ExecuteScalar<int>(cmd);
            return count == 1;
        }

        public override void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns, bool generateConstraints = true)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                _transaction = transaction;
                try
                {
                    base.CreateTable(table, columns, generateConstraints);
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                _transaction = null;
            }
        }

        public override void RenameTable(TableAttribute oldName, TableAttribute newName)
        {
            var cmd = _connection.CreateCommand();
            cmd.CommandText = string.Format("EXEC sp_rename '{0}', '{1}'", oldName.FullName, newName.Name);
            cmd.ExecuteNonQuery();
        }

        public override void DeleteTable(TableAttribute table)
        {
            var drop = new DropTableExpression(new TableExpression(table));
            ExecuteNonQuery(drop);
        }

        public override void CommitChanges(IEnumerable entities, TableAttribute table, ColumnInfoCollection columns)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                _transaction = transaction;
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
                                    var idCommand = string.Format("SELECT CAST(@@IDENTITY AS {0})", pk.ColumnType);
                                    pk.SetValue(entity, ExecuteScalar(idCommand));
                                }
                                entity.State = EntityState.Unchanged;
                                break;
                            case EntityState.Changed:
                                Update(entity, table, columns);
                                break;
                            case EntityState.Deleted:
                                Delete(entity, table, columns);
                                break;
                        }
                    }
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
                _transaction = null;
            }
        }

        public override string GetLiteral(object value)
        {
            if (value == null) return "null";
            return ColumnMapper.MapToSql(value);
        }

        public override IEnumerable ExecuteQuery(Type elementType, Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return ExecuteQuery(elementType, visitor.CommandText);
        }

        public override void ExecuteNonQuery(Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            ExecuteNonQuery(visitor.CommandText);
        }

        public override object ExecuteScalar(Expression expression, Type type)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return ExecuteScalar(visitor.CommandText);
        }

        private IEnumerable ExecuteQuery(Type type, string command)
        {
            Console.WriteLine(command);

            var columns = ColumnMapper.Cache.ContainsKey(type) ? ColumnMapper.Cache[type] : null;
            var listType = typeof(List<>).MakeGenericType(type);
            var addMethod = listType.GetMethod("Add");
            var list = (IEnumerable)Activator.CreateInstance(listType);

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
                    addMethod.Invoke(list, new[] { instance });
                }
                reader.Close();
            }

            return list;
        }

        private void ExecuteNonQuery(string command)
        {
            Console.WriteLine(command);

            using (var cmd = new SqlCommand(command, _connection))
            {
                cmd.Transaction = _transaction;
                cmd.ExecuteNonQuery();
            }
        }

        private T ExecuteScalar<T>(string command)
        {
            return (T)ExecuteScalar(command);
        }

        private object ExecuteScalar(string command)
        {
            Console.WriteLine(command);

            using (var cmd = new SqlCommand(command, _connection))
            {
                cmd.Transaction = _transaction;
                var value = cmd.ExecuteScalar();
                return ColumnMapper.MapToType(value);
            }
        }

        public override void Dispose()
        {
            _connection.Dispose();
        }
    }
}
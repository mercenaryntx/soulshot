using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
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
            var count = _connection.ExecuteScalar<int>(string.Format("SELECT count(*) FROM master.dbo.sysdatabases WHERE name = '{0}'", database));
            if (count == 0)
            {
                _connection.ExecuteNonQuery(string.Format("CREATE DATABASE {0}", database));
            }
            _connection.ChangeDatabase(database);
        }

        public override bool TableExists(TableAttribute table)
        {
            var cmd = string.Format(@"select count(*) from sys.objects o
									  inner join sys.schemas s on s.schema_id = o.schema_id
									  where type = 'U' and o.name = '{0}' and s.name = '{1}'", table.Name, table.Schema);
            var count = _connection.ExecuteScalar<int>(cmd);
            return count == 1;
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

        public override void CommitChanges(IEnumerable entities, TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                _transaction = transaction;
                try
                {
                    foreach (IProxy entity in entities)
                    {
                        switch (entity.State)
                        {
                            case EntityState.Added:
                                Insert(entity, table, columns);
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

            //TODO: proper mapping
            var type = value.GetType();
            if (type.IsClass) return "N'" + value + "'";
            int? intValue = null;
            if (type.IsEnum) intValue = (int)value;
            if (type == typeof (Boolean)) intValue = ((bool) value) ? 1 : 0;
            return intValue.HasValue ? intValue.ToString() : value.ToString();
        }

        public override void ExecuteNonQuery(Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            _connection.ExecuteNonQuery(visitor.CommandText, _transaction);
        }

        public override IEnumerable Execute(Type elementType, Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return _connection.ExecuteQuery(elementType, visitor.CommandText, _transaction);
        }

        public override void Dispose()
        {
            _connection.Dispose();
        }
    }
}
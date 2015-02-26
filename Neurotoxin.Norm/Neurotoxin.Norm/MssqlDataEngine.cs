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

        protected override void CreateTable(TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            var cmd = _connection.CreateCommand();
            var columnDefinitions = string.Join(",", columns.Select(c => c.DefinitionString));
            //TODO: proper PK handling
            var identityColumns = columns.Where(c => c.IsIdentity).Select(c => string.Format("[{0}] ASC", c.ColumnName)).ToList();
            var primaryKeyConstraint = identityColumns.Any()
                ? string.Format(",CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED ({1})", table.FullName, string.Join(",", identityColumns))
                : string.Empty;
            cmd.CommandText = string.Format("CREATE TABLE {0} ({1}{2})", table.FullNameWithBrackets, columnDefinitions, primaryKeyConstraint);
            cmd.ExecuteNonQuery();
        }

        public override void RenameTable(TableAttribute oldName, TableAttribute newName)
        {
            //throw new NotImplementedException();
        }

        public override void CommitChanges(IEnumerable entities, TableAttribute table, IEnumerable<ColumnInfo> columns)
        {
            using (var transaction = _connection.BeginTransaction())
            {
                try
                {
                    foreach (IProxy entity in entities)
                    {
                        switch (entity.State)
                        {
                            case EntityState.Added:
                                Insert(entity, table, columns, transaction);
                                break;
                            case EntityState.Changed:
                                Update(entity, table, columns, transaction);
                                break;
                            case EntityState.Deleted:
                                Delete(entity, table, columns, transaction);
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
            }
        }

        public override string GetLiteral(object value)
        {
            //TODO: 
            var isString = value is string;
            if (isString) return "N'" + value + "'";
            return value.ToString();
        }

        private void Insert(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns, SqlTransaction transaction = null)
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            var columnNames = new StringBuilder();
            var values = new StringBuilder();
            foreach (var column in columns.Where(c => !c.IsIdentity))
            {
                if (columnNames.Length != 0)
                {
                    columnNames.Append(",");
                    values.Append(",");
                }
                columnNames.Append(column.ColumnName);
                values.Append(GetLiteral(column.BaseType.GetProperty(column.PropertyName).GetValue(entity)));
            }
            cmd.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", table.FullNameWithBrackets, columnNames, values);
            cmd.ExecuteNonQuery();
        }

        private void Update(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns, SqlTransaction transaction = null)
        {
            var cmd = _connection.CreateCommand();
            cmd.Transaction = transaction;
            foreach (var tpp in entity.DirtyProperties)
            {
                var column = columns.First(c => c.BaseType == tpp.Type && c.PropertyName == tpp.Property);
                throw new NotImplementedException();
            }            
        }

        private void Delete(IProxy entity, TableAttribute table, IEnumerable<ColumnInfo> columns, SqlTransaction transaction = null)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable Execute(Type elementType, Expression expression)
        {
            var visitor = new SqlCommandTextVisitor(this);
            visitor.Visit(expression);
            return _connection.ExecuteQuery(elementType, visitor.CommandText);
        }

        public override void Dispose()
        {
            _connection.Dispose();
        }
    }
}
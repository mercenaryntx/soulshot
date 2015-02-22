using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;

namespace Neurotoxin.Norm
{
    public abstract class DbContext : IDisposable
    {
        private readonly IDataEngine _dataEngine;

        protected DbContext(string connectionString)
        {
            _dataEngine = new MssqlDataEngine(connectionString);
            var migrationHistory = new DbSet<ColumnInfo>(_dataEngine);

            var iDbSet = typeof(IDbSet);
            foreach (var pi in GetType().GetProperties().Where(pi => iDbSet.IsAssignableFrom(pi.PropertyType)))
            {
                var table = GetTableDefinition(pi);
                var columns = migrationHistory.Where(e => e.TableName == table.Name && e.TableSchema == table.Schema).ToList();
                var dbSet = CreateDbSet(pi, table, columns);
                if (dbSet.Columns != columns)
                {
                    migrationHistory.Remove(e => e.TableName == table.Name && e.TableSchema == table.Schema);
                    foreach (var column in dbSet.Columns)
                    {
                        var c = migrationHistory.Add(column);
                    }
                    migrationHistory.SaveChanges();
                }
                pi.SetValue(this, dbSet);
            }
        }

        private TableAttribute GetTableDefinition(PropertyInfo pi)
        {
            return pi.GetAttribute<TableAttribute>() ??
                   pi.PropertyType.GetGenericArguments().First().GetAttribute<TableAttribute>() ??
                   new TableAttribute(pi.Name);
        }

        private IDbSet CreateDbSet(PropertyInfo pi, TableAttribute table, List<ColumnInfo> columns)
        {
            var ctor = pi.PropertyType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { table.GetType(), columns.GetType(), typeof(IDataEngine) }, null);
            return (IDbSet)ctor.Invoke(new object[] { table, columns, _dataEngine });
        }

        public void Dispose()
        {
            _dataEngine.Dispose();
        }
    }
}
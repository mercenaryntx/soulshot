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
        private readonly DbSet<ColumnInfo> _migrationHistory;
        private readonly List<IDbSet> _dbSets = new List<IDbSet>();

        protected DbContext(string connectionString)
        {
            _dataEngine = new MssqlDataEngine(connectionString);
            _migrationHistory = new DbSet<ColumnInfo>(_dataEngine);
            _migrationHistory.Init();

            var iDbSet = typeof(IDbSet);
            var dbSetProperties = GetType().GetProperties().Where(pi => iDbSet.IsAssignableFrom(pi.PropertyType)).ToList();
            foreach (var pi in dbSetProperties)
            {
                var table = GetTableDefinition(pi);
                var dbSet = EnsureTable(pi.PropertyType, table);
                pi.SetValue(this, dbSet);
            }
        }

        private TableAttribute GetTableDefinition(PropertyInfo pi)
        {
            return pi.GetAttribute<TableAttribute>() ?? pi.PropertyType.GetGenericArguments().First().GetTableAttribute();
        }

        private IDbSet EnsureTable(Type type, TableAttribute table)
        {
            var columns = _migrationHistory.Where(e => e.TableName == table.Name && e.TableSchema == table.Schema).ToList();
            var dbSet = CreateDbSet(type, table, columns);
            _dbSets.Add(dbSet);
            if (!dbSet.Columns.SequenceEqual(columns))
            {
                _migrationHistory.Remove(e => e.TableName == table.Name && e.TableSchema == table.Schema);
                foreach (var column in dbSet.Columns)
                {
                    _migrationHistory.Add(column);
                }
                _migrationHistory.SaveChanges();
            }
            return dbSet;
        }

        private IDbSet CreateDbSet(Type type, TableAttribute table, List<ColumnInfo> columns)
        {
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { table.GetType(), columns.GetType(), typeof(IDataEngine) }, null);
            var instance = (IDbSet)ctor.Invoke(new object[] { table, columns, _dataEngine });
            instance.Init(ResolveDependencies);
            return instance;
        }

        private List<IDbSet> ResolveDependencies(List<ColumnInfo> columns)
        {
            return columns.Where(c => c.ReferenceTable != null)
                          .Select(column => EnsureTable(typeof(DbSet<>).MakeGenericType(column.ReferenceTable), column.ReferenceTable.GetTableAttribute()))
                          .ToList();
        }

        public void SaveChanges()
        {
            _dbSets.ForEach(d => d.SaveChanges());
        }

        public void Dispose()
        {
            _dataEngine.Dispose();
        }
    }
}
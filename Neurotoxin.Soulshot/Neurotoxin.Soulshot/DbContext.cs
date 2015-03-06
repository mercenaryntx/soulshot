using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot
{
    public abstract class DbContext : IDisposable
    {
        private readonly DbSet<ColumnInfo> _migrationHistory;
        private readonly List<IDbSet> _dbSets = new List<IDbSet>();
        private readonly Type _iDbSet = typeof(IDbSet);

        internal IDataEngine DataEngine { get; set; }

        protected DbContext(string connectionString)
        {
            DataEngine = new MssqlDataEngine(connectionString);
            DataEngine.ColumnMapper.Context = this;
            _migrationHistory = CreateDbSet<ColumnInfo>();
            _dbSets.Add(_migrationHistory);
            
            var dbSetProperties = GetType().GetProperties().Where(pi => _iDbSet.IsAssignableFrom(pi.PropertyType)).ToList();
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

        internal IDbSet EnsureTable(Type type, TableAttribute table = null)
        {
            if (table == null) table = type.GetTableAttribute();
            var existing = GetDbSet(table);
            if (existing != null) return existing;

            var columns = new ColumnInfoCollection(type, table, _migrationHistory.Where(e => e.TableName == table.Name && e.TableSchema == table.Schema));
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

        private DbSet<T> CreateDbSet<T>()
        {
            var type = typeof(T);
            return (DbSet<T>)CreateDbSet(GetDbSetType(type), type.GetTableAttribute(), null);
        }

        private IDbSet CreateDbSet(Type type, TableAttribute table, ColumnInfoCollection columns)
        {
            if (!_iDbSet.IsAssignableFrom(type)) type = GetDbSetType(type);
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(TableAttribute), typeof(ColumnInfoCollection), typeof(DbContext) }, null);
            var instance = (IDbSet)ctor.Invoke(new object[] { table, columns, this });
            instance.Init();
            return instance;
        }

        private Type GetDbSetType(Type entityType)
        {
            return typeof(DbSet<>).MakeGenericType(entityType);
        }

        public IDbSet GetDbSet(Type type)
        {
            return GetDbSet(type.GetTableAttribute());
        }

        public IDbSet GetDbSet(TableAttribute table)
        {
            return _dbSets.SingleOrDefault(d => d.Table.Equals(table));
        }

        public void SaveChanges()
        {
            _dbSets.ForEach(d => d.SaveChanges());
        }

        public void Dispose()
        {
            DataEngine.Dispose();
        }
    }
}
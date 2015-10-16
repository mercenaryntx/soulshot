using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot
{
    public abstract class DbContext : IDisposable
    {
        //private readonly bool _codeFirst;
        private DbSet<ColumnInfo> _migrationHistory;
        private readonly List<IDbSet> _dbSets = new List<IDbSet>();
        private readonly Type _iDbSet = typeof(IDbSet);
        private readonly DbModelBuilder _modelBuilder;

        internal IDataEngine DataEngine { get; private set; }

        protected DbContext(string connectionString, bool? codeFirst = null)
        {
            //_codeFirst = codeFirst ?? ConfigurationManager.AppSettings["CodeFirst"] != "false";
            DataEngine = new MssqlDataEngine(connectionString, GetType().Assembly) {ColumnMapper = {Context = this}};

            _modelBuilder = new DbModelBuilder(this);
            _modelBuilder.Entity<ColumnInfo>();
            _modelBuilder.Build((dbset, columns) => _dbSets.Add(_migrationHistory = (DbSet<ColumnInfo>)dbset));
            CreateModel();
        }

        private void CreateModel()
        {
            var dbSetProperties = GetType().GetProperties().Where(pi => _iDbSet.IsAssignableFrom(pi.PropertyType)).ToList();
            var properties = new Dictionary<IColumnInfoCollection, PropertyInfo>();
            foreach (var pi in dbSetProperties)
            {
                var table = GetTableDefinition(pi);
                var columns = _modelBuilder.Entity(pi.PropertyType.GetGenericArguments()[0], table);
                properties.Add(columns, pi);
            }
            OnModelCreating(_modelBuilder);
            foreach (var dbSet in _modelBuilder.Build(UpdateMigrationHistoryIfNecessary))
            {
                if (properties.ContainsKey(dbSet.Columns))
                    properties[dbSet.Columns].SetValue(this, dbSet);
            }
            _migrationHistory.SaveChanges();
        }

        protected virtual void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        private TableAttribute GetTableDefinition(PropertyInfo pi)
        {
            return pi.GetAttribute<TableAttribute>() ?? pi.PropertyType.GetGenericArguments().First().GetTableAttribute();
        }

        private void UpdateMigrationHistoryIfNecessary(IDbSet dbSet, IColumnInfoCollection columns)
        {
            _dbSets.Add(dbSet);
            var table = dbSet.Table;
            var storedColumns = _migrationHistory.Where(e => e.TableName == table.Name && e.TableSchema == table.Schema).ToList();
            if (!columns.SequenceEqual(storedColumns))
            {
                dbSet.UpdateTable(storedColumns);
                //TODO: this is an immediate delete!
                _migrationHistory.Delete(e => e.TableName == table.Name && e.TableSchema == table.Schema);
                foreach (var column in dbSet.Columns)
                {
                    _migrationHistory.Add(column);
                }
            }
        }

        internal IDbSet EnsureTable(Type type, TableAttribute table = null)
        {
            if (table == null) table = type.GetTableAttribute();
            var existing = GetDbSet(table);
            if (existing != null) return existing;
            //if (!_codeFirst) throw new Exception("Table not exists: " + table.FullName);

            _modelBuilder.Entity(type, table);
            var dbSet = _modelBuilder.Build().First();
            return dbSet;
        }

        public IDbSet GetDbSet(Type type)
        {
            return GetDbSet(type.GetTableAttribute());
        }

        public IDbSet GetDbSet(TableAttribute table)
        {
            return _dbSets.SingleOrDefault(d => d.Table.Equals(table));
        }

        public DbSet<T> Set<T>()
        {
            return (DbSet<T>)GetDbSet(typeof(T).GetTableAttribute());
        }

        public void SaveChanges()
        {
            _dbSets.ForEach(d => d.SaveChanges());
        }

        public Task SaveChangesAsync()
        {
            return Task.Run(() => SaveChanges());
        }

        public IEnumerable<T> ExecuteQuery<T>(string command, params SqlParameter[] args)
        {
            return DataEngine.ExecuteQuery<T>(command, args);
        }

        public void Dispose()
        {
            DataEngine.Dispose();
        }
    }
}
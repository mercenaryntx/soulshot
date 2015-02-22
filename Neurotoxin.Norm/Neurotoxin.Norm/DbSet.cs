using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    public class DbSet<TEntity> : IDbSet, IQueryable<TEntity> //where TEntity : class
    {
        private TableAttribute _table;
        private IDataEngine _dataEngine;
        private List<ColumnInfo> _keyColumns;
        private readonly List<TEntity> _cachedEntities = new List<TEntity>();

        public List<ColumnInfo> Columns { get; private set; }
        public bool TableUpdated { get; private set; }

        internal DbSet(IDataEngine dataEngine)
        {
            Init(typeof(TEntity).GetTableAttribute(), null, dataEngine);
        }

        internal DbSet(TableAttribute table, List<ColumnInfo> columns, IDataEngine dataEngine)
        {
            Init(table, columns, dataEngine);
        }

        public DbSet(SqlQueryProvider provider, Expression expression)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (expression == null) throw new ArgumentNullException("expression");
            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type)) throw new ArgumentOutOfRangeException("expression");

            _dataEngine = provider.DataEngine;
            _table = provider.Table;
            Columns = provider.Columns;
            Provider = provider;
            Expression = expression;
        }

        private void Init(TableAttribute table, List<ColumnInfo> columns, IDataEngine dataEngine)
        {
            _table = table;
            _dataEngine = dataEngine;
            Columns = _dataEngine.UpdateTable<TEntity>(_table, columns);
            _keyColumns = Columns.Where(c => c.IsIdentity).ToList();
            Provider = new SqlQueryProvider(dataEngine, table, Columns);
            Expression = Expression.Constant(this);
        }

        public TEntity Add(TEntity entity, EntityState state = EntityState.Added)
        {
            var proxy = entity as IProxy ?? (IProxy)DynamicProxy.Instance.Wrap(entity);
            //var key = GetKey(entity);
            entity = (TEntity) proxy;
            _cachedEntities.Add(entity);
            proxy.State = state;
            return entity;
        }

        public void Remove(Func<TEntity, bool> expression)
        {
            //throw new NotImplementedException();
        }

        public void SaveChanges()
        {
            Debugger.Break();
        }

        #region IQueryable members

        public Expression Expression { get; private set; }
        public IQueryProvider Provider { get; private set; }

        public Type ElementType
        {
            get { return typeof(TEntity); }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            var resultSet = Provider.Execute<List<TEntity>>(Expression);
            foreach (var entity in resultSet)
            {
                Add(entity, EntityState.Unchanged);
            }
            return resultSet.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        private EntityKey GetKey(TEntity entity)
        {
            return new EntityKey(_keyColumns.Select(c => c.BaseType.GetProperty(c.PropertyName).GetValue(entity, null)));
        }

    }
}
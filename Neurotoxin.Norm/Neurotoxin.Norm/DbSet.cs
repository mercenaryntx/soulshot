using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    public class DbSet<TEntity> : IDbSet, IOrderedQueryable<TEntity>
    {
        private readonly TableAttribute _table;
        private readonly IDataEngine _dataEngine;
        private readonly List<TEntity> _cachedEntities = new List<TEntity>();

        public List<ColumnInfo> Columns { get; private set; }
        public SqlQueryProvider Provider { get; private set; }

        internal DbSet(IDataEngine dataEngine)
        {
            _table = typeof (TEntity).GetTableAttribute();
            _dataEngine = dataEngine;
        }

        internal DbSet(TableAttribute table, List<ColumnInfo> columns, IDataEngine dataEngine)
        {
            _table = table;
            Columns = columns;
            _dataEngine = dataEngine;
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

        public void Init()
        {
            Columns = _dataEngine.UpdateTable<TEntity>(_table, Columns);
            Provider = new SqlQueryProvider(_dataEngine, _table, Columns);
            Expression = Expression.Constant(this);
        }

        public TEntity Add(TEntity entity)
        {
            return Add(entity, EntityState.Added);
        }

        internal TEntity Add(TEntity entity, EntityState state)
        {
            var proxy = entity as IProxy ?? (IProxy)DynamicProxy.Instance.Wrap(entity);
            entity = (TEntity)proxy;
            _cachedEntities.Add(entity);
            proxy.State = state;
            return entity;
        }

        public void Remove(Expression<Func<TEntity, bool>> expression)
        {
            Provider.Delete(expression);
        }

        public void SaveChanges()
        {
            if (_cachedEntities.All(e => ((IProxy)e).State == EntityState.Unchanged)) return;
            _dataEngine.CommitChanges(_cachedEntities, _table, Columns);
        }

        #region IQueryable members

        public Expression Expression { get; private set; }
        IQueryProvider IQueryable.Provider { get { return Provider; } }

        public Type ElementType
        {
            get { return typeof(TEntity); }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            var resultSet = Provider.Select<TEntity>(Expression);
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

    }
}
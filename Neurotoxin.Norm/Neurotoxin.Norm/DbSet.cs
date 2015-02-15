using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Principal;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm
{
    public class DbSet<TEntity> : IDbSet, IQueryable<TEntity>
        //, IList<TEntity>
    {
        private TableAttribute _table;
        private IDataEngine _dataEngine;

        private List<TEntity> _cachedEntities;

        protected List<TEntity> CachedEntities
        {
            get { return _cachedEntities ?? (_cachedEntities = new List<TEntity>()); }
        } 

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
            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type))
                throw new ArgumentOutOfRangeException("expression");

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
            Provider = new SqlQueryProvider(dataEngine, table, Columns);
            Expression = Expression.Constant(this);
        }

        public void SaveChanges()
        {
            
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
            return (Provider.Execute<IEnumerable<TEntity>>(Expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IList members

        public void Add(TEntity item)
        {
            CachedEntities.Add(item);
        }

        public void Clear()
        {
            CachedEntities.Clear();
        }

        public bool Contains(TEntity item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(TEntity item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public int IndexOf(TEntity item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, TEntity item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public TEntity this[int index]
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        #endregion
    }
}
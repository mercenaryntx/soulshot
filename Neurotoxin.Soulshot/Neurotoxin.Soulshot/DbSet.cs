using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class DbSet<TEntity> : IDbSet, IOrderedQueryable<TEntity>
    {
        private IDataEngine _dataEngine;
        private readonly HashSet<TEntity> _cachedEntities = new HashSet<TEntity>();
        private HashSet<IDbSet> _relatedDbSets;

        public TableAttribute Table { get; private set; }
        public ColumnInfoCollection Columns { get; private set; }
        public SqlQueryProvider Provider { get; private set; }

        private DbContext _context;
        public DbContext Context
        {
            get { return _context; }
            private set
            {
                _context = value;
                _dataEngine = value.DataEngine;
            }
        }

        public Type EntityType
        {
            get { return typeof(TEntity); }
        }

        public ColumnInfo PrimaryKey
        {
            get { return Columns.SingleOrDefault(c => c.IsIdentity); }
        }

        internal DbSet(TableAttribute table, ColumnInfoCollection columns, DbContext context)
        {
            Table = table;
            Columns = columns;
            Context = context;
        }

        public DbSet(SqlQueryProvider provider, Expression expression)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (expression == null) throw new ArgumentNullException("expression");
            if (!typeof(IQueryable<TEntity>).IsAssignableFrom(expression.Type)) throw new ArgumentOutOfRangeException("expression");

            Table = provider.DbSet.Table;
            Context = provider.DbSet.Context;
            Columns = provider.DbSet.Columns;
            Provider = provider;
            Expression = expression;
        }

        public void Init()
        {
            var actualColumns = _dataEngine.ColumnMapper.Map<TEntity>(Table);

            _relatedDbSets = new HashSet<IDbSet>(actualColumns.Where(c => c.ReferenceTable != null).Select(c => _context.GetDbSet(c.ReferenceTable.BaseType)));
            Columns = _dataEngine.UpdateTable<TEntity>(Table, actualColumns, Columns);
            Provider = new SqlQueryProvider(_dataEngine, this);
            Expression = Expression.Constant(this);
        }

        public TEntity Add(TEntity entity)
        {
            return Add(entity, EntityState.Added);
        }

        internal TEntity Add(TEntity entity, EntityState state)
        {
            var existingProxy =_cachedEntities.SingleOrDefault(e => ((IEntityProxy) e).GeneratedFrom.Equals(entity));
            if (existingProxy != null) return existingProxy;

            var proxy = entity as IEntityProxy ?? (IEntityProxy)DynamicProxy.Instance.Wrap(entity);
            entity = (TEntity)proxy;

            _cachedEntities.Add(entity);
            proxy.State = state;
            foreach (var column in Columns.Where(c => c.ReferenceTable != null))
            {
                var reference = column.GetValue(entity);
                if (reference == null) continue;
                var referenceEntityType = reference.GetType();
                var referenceDbSetType = typeof(DbSet<>).MakeGenericType(referenceEntityType);
                var referenceDbSet = _relatedDbSets.Single(d => d.GetType() == referenceDbSetType);
                var add = referenceDbSetType.GetMethod("Add", new[] { referenceEntityType });
                var referenceProxy = add.Invoke(referenceDbSet, new[] { reference });
                column.SetValue(entity, referenceProxy);
            }
            return entity;
        }

        public void Remove(Expression<Func<TEntity, bool>> expression)
        {
            Provider.Delete(expression);
        }

        public void SaveChanges()
        {
            SaveChanges(_cachedEntities);
            _cachedEntities.Clear();
        }

        public void CacheEntity(object entity)
        {
            if (entity == null) return;
            Add((TEntity) entity, EntityState.Unchanged);
        }

        public void CacheEntities(IEnumerable entities)
        {
            foreach (var entity in entities.Cast<TEntity>())
            {
                Add(entity, EntityState.Unchanged);
            }
        }

        public void SaveChanges(IEnumerable<TEntity> entities)
        {
            if (entities.All(e => ((IEntityProxy)e).State == EntityState.Unchanged)) return;
            if (_relatedDbSets != null)
            {
                foreach (var dbSet in _relatedDbSets)
                {
                    dbSet.SaveChanges();
                }
            }

            _dataEngine.CommitChanges(entities, Table, Columns);
        }

        public void BulkInsert(IEnumerable<TEntity> entities)
        {
            _dataEngine.BulkInsert(entities, Table, Columns);
        }

        //public TEntity SingleById(object id)
        //{
        //    var pk = Columns.Single(c => c.IsIdentity).ToColumnExpression(); //TODO: support complex keys
        //    var select = new SelectExpression(new TableExpression(Table));
        //    select.Where = Expression.MakeBinary(ExpressionType.Equal, pk, Expression.Constant(id, pk.Type));
        //    return _dataEngine.Execute<TEntity>(select).Single();
        //}

        #region IQueryable members

        public Expression Expression { get; private set; }
        IQueryProvider IQueryable.Provider { get { return Provider; } }

        public Type ElementType
        {
            get { return typeof(TEntity); }
        }

        public IEnumerator<TEntity> GetEnumerator()
        {
            return Provider.Select<TEntity>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;

namespace Neurotoxin.Soulshot
{
    public class DbModelBuilder
    {
        private readonly List<Tuple<Type, TableAttribute, IColumnInfoCollection>> _models = new List<Tuple<Type, TableAttribute, IColumnInfoCollection>>();
        private readonly Type _iDbSet = typeof(IDbSet);
        private readonly DbContext _dbContext;

        public DbModelBuilder(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IColumnInfoCollection Entity(Type type, TableAttribute table = null)
        {
            if (table == null) table = type.GetAttribute<TableAttribute>();
            if (table == null) table = new TableAttribute(type.Name);
            var columns = _dbContext.DataEngine.ColumnMapper.Map(type, table);
            //var columns = (IColumnInfoCollection) typeof (ColumnInfoCollection<>).MakeGenericType(type)
            //    .GetConstructors()
            //    .First()
            //    .Invoke(new object[] {table, null});
            var tuple = new Tuple<Type, TableAttribute, IColumnInfoCollection>(type, table, columns);
            _models.Add(tuple);
            return columns;
        }

        public ColumnInfoCollection<T> Entity<T>()
        {
            var type = typeof(T);
            return (ColumnInfoCollection<T>)Entity(type);
        }

        public IEnumerable<IDbSet> Build(Action<IDbSet, IColumnInfoCollection> externalAction = null)
        {
            var result = _models.Select(tuple =>
            {
                var dbSet = CreateDbSet(tuple.Item1, tuple.Item2, tuple.Item3);
                if (externalAction != null) externalAction.Invoke(dbSet, tuple.Item3);
                return dbSet;
            }).ToList();
            _models.Clear();
            return result;
        }

        private IDbSet CreateDbSet(Type type, TableAttribute table, IColumnInfoCollection columns)
        {
            if (!_iDbSet.IsAssignableFrom(type)) type = GetDbSetType(type);
            var ctor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(TableAttribute), typeof(IColumnInfoCollection), typeof(DbContext) }, null);
            var instance = (IDbSet)ctor.Invoke(new object[] { table, columns, _dbContext });
            instance.Init();
            return instance;
        }

        private Type GetDbSetType(Type entityType)
        {
            return typeof(DbSet<>).MakeGenericType(entityType);
        }
    }
}
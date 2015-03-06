using System;
using System.Linq;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class ForeignKeyMapper<T> : MapperBase
    {
        private readonly DbSet<T> _dbSet;

        //TODO: use IoC
        public ForeignKeyMapper(ForeignKeyAttribute a, DbSet<T> dbSet) : base(typeof(T), a)
        {
            _dbSet = dbSet;
        }

        public override object MapToType(object value, Type type)
        {
            //var proxy = DynamicProxy.Instance.CreateLazy<T>(type);
            //proxy.DbSet = _dbSet;
            var proxy = DynamicProxy.Instance.Create(type);
            _dbSet.PrimaryKey.SetValue(proxy, value);
            return proxy;
        }

        public override string MapToSql(object value)
        {
            var pk = _dbSet.PrimaryKey;
            return base.MapToSql(pk.GetValue(value));
        }
    }
}
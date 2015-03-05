using System.Linq;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class ForeignKeyMapper<T> : MapperBase
    {
        public ForeignKeyMapper(ForeignKeyAttribute a) : base(typeof(T), a)
        {
        }

        public override string MapToSql(object value)
        {
            var columns = ColumnMapper.Cache[typeof(T)];
            var pk = columns.First(c => c.IsIdentity); //TODO: support complex keys
            return base.MapToSql(pk.GetValue(value));
        }
    }
}
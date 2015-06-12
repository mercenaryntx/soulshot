using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class Int64Mapper : MapperBase
    {
        public Int64Mapper() : base(typeof(Int64), new BigIntAttribute())
        {
        }

        public override object MapToType(object value, Type type)
        {
            if (value is int) return Convert.ToInt64(value);
            return base.MapToType(value, type);
        }
    }
}
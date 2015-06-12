using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class UInt64Mapper : MapperBase
    {
        public UInt64Mapper() : base(typeof(UInt64), new BigIntAttribute())
        {
        }

        public override object MapToType(object value, Type type)
        {
            if (value is int) return Convert.ToUInt64(value);
            return base.MapToType(value, type);
        }
    }
}
using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class NullableInt32Mapper : MapperBase
    {
        public NullableInt32Mapper() : base(typeof(Int32?), new IntegerAttribute())
        {
        }

        public override object MapToType(object value, Type type)
        {
            if (value is int) return value;
            return base.MapToType(value, type);
        }

    }
}
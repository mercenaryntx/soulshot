using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class BooleanMapper : MapperBase
    {
        public BooleanMapper() : base(typeof(Boolean), new BooleanAttribute())
        {
        }

        public override object MapToSql(object value)
        {
            return ((bool) value) ? 1 : 0;
        }
    }
}
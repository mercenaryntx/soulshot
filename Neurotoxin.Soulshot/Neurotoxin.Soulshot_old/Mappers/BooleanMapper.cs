using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class BooleanMapper : MapperBase
    {
        public BooleanMapper() : base(typeof(Boolean), new BooleanAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            return ((bool) value) ? "1" : "0";
        }
    }
}
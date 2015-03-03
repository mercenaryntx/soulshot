using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class DateTimeMapper : MapperBase
    {
        public DateTimeMapper() : base(typeof(DateTime), new DateTime2Attribute())
        {
        }

        public override string MapToSql(object value)
        {
            return string.Format("'{0}'", value);
        }
    }
}
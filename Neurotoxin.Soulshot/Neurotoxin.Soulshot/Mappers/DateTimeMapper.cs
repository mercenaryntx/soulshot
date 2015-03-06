using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
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
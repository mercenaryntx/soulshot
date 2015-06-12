using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class TimeSpanMapper : MapperBase
    {
        public TimeSpanMapper() : base(typeof(TimeSpan), new TimeAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            return string.Format("'{0}'", value);
        }
    }
}
using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class DateTimeOffsetMapper : MapperBase
    {
        public DateTimeOffsetMapper() : base(typeof(DateTimeOffset), new DateTimeOffsetAttribute())
        {
        }
    }
}
using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class DateTimeOffsetMapper : MapperBase
    {
        public DateTimeOffsetMapper() : base(typeof(DateTimeOffset), new DateTimeOffsetAttribute())
        {
        }
    }
}
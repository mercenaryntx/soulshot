using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class TimeSpanMapper : MapperBase
    {
        public TimeSpanMapper() : base(typeof(TimeSpan), new TimeAttribute())
        {
        }
    }
}
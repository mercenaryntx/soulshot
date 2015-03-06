using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class TimeSpanMapper : MapperBase
    {
        public TimeSpanMapper() : base(typeof(TimeSpan), new TimeAttribute())
        {
        }
    }
}
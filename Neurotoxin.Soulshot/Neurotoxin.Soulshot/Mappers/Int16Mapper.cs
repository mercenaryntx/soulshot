using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class Int16Mapper : MapperBase
    {
        public Int16Mapper() : base(typeof(Int16), new SmallIntAttribute())
        {
        }
    }
}
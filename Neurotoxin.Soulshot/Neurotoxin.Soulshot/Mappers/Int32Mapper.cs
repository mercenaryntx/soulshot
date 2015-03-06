using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class Int32Mapper : MapperBase
    {
        public Int32Mapper() : base(typeof(Int32), new IntegerAttribute())
        {
        }
    }
}
using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class UInt32Mapper : MapperBase
    {
        public UInt32Mapper() : base(typeof(UInt32), new IntegerAttribute())
        {
        }
    }
}
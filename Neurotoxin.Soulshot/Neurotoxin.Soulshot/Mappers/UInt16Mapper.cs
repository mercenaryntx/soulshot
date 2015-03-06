using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class UInt16Mapper : MapperBase
    {
        public UInt16Mapper() : base(typeof(UInt16), new SmallIntAttribute())
        {
        }
    }
}
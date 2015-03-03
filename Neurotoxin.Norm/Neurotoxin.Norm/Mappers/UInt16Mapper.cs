using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class UInt16Mapper : MapperBase
    {
        public UInt16Mapper() : base(typeof(UInt16), new SmallIntAttribute())
        {
        }
    }
}
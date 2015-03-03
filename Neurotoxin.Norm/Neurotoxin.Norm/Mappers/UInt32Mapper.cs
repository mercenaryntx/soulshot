using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class UInt32Mapper : MapperBase
    {
        public UInt32Mapper() : base(typeof(UInt32), new IntegerAttribute())
        {
        }
    }
}
using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class UInt64Mapper : MapperBase
    {
        public UInt64Mapper() : base(typeof(UInt64), new BigIntAttribute())
        {
        }
    }
}
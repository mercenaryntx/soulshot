using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class Int32Mapper : MapperBase
    {
        public Int32Mapper() : base(typeof(Int32), new IntegerAttribute())
        {
        }
    }
}
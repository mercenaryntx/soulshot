using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class Int16Mapper : MapperBase
    {
        public Int16Mapper() : base(typeof(Int16), new SmallIntAttribute())
        {
        }
    }
}
using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class SingleMapper : MapperBase
    {
        public SingleMapper() : base(typeof(Single), new FloatAttribute())
        {
        }
    }
}
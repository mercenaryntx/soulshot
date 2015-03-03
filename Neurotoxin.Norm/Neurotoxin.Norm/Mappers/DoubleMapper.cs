using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class DoubleMapper : MapperBase
    {
        public DoubleMapper() : base(typeof(Double), new FloatAttribute())
        {
        }
    }
}
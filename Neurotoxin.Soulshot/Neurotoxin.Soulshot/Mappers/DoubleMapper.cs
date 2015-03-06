using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class DoubleMapper : MapperBase
    {
        public DoubleMapper() : base(typeof(Double), new FloatAttribute())
        {
        }
    }
}
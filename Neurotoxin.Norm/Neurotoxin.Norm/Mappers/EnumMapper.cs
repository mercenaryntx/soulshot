using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class EnumMapper : MapperBase
    {
        public EnumMapper() : base(typeof(Enum), new IntegerAttribute())
        {
        }
    }
}
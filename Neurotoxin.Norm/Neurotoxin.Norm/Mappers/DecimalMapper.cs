using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class DecimalMapper : MapperBase
    {
        public DecimalMapper() : base(typeof(Decimal), new DecimalAttribute())
        {
        }
    }
}
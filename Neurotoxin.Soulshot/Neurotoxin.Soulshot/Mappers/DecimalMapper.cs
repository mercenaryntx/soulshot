using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class DecimalMapper : MapperBase
    {
        public DecimalMapper() : base(typeof(Decimal), new DecimalAttribute())
        {
        }
    }
}
using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class SingleMapper : MapperBase
    {
        public SingleMapper() : base(typeof(Single), new FloatAttribute())
        {
        }
    }
}
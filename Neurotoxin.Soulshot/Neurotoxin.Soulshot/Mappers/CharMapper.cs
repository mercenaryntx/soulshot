using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class CharMapper : MapperBase
    {
        public CharMapper() : base(typeof(Char), new CharAttribute(1))
        {
        }
    }
}
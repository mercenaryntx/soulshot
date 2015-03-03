using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class CharMapper : MapperBase
    {
        public CharMapper() : base(typeof(Char), new CharAttribute(1))
        {
        }
    }
}
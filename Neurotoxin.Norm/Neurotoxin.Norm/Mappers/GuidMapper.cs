using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class GuidMapper : MapperBase
    {
        public GuidMapper() : base(typeof(Guid), new UniqueIdentifierAttribute())
        {
        }
    }
}
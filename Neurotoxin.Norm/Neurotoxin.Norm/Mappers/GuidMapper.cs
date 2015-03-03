using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class GuidMapper : MapperBase
    {
        public GuidMapper() : base(typeof(Guid), new UniqueIdentifierAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            return string.Format("'{0}'", value);
        }
    }
}
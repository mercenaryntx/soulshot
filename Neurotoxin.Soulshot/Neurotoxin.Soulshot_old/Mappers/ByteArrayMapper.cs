using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class ByteArrayMapper : MapperBase
    {
        public ByteArrayMapper() : base(typeof(byte[]), new VarbinaryAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            throw new NotImplementedException();
        }
    }
}
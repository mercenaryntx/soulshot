using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class ByteArrayMapper : MapperBase
    {
        public ByteArrayMapper() : base(typeof(byte[]), new VarbinaryAttribute())
        {
        }
    }
}
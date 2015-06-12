using System;
using System.Globalization;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class DoubleMapper : MapperBase
    {
        public DoubleMapper() : base(typeof(Double), new FloatAttribute())
        {
        }

        public override string MapToSql(object value)
        {
            var d = (double) value;
            return d.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
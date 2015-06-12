using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class StringMapper : MapperBase
    {
        public StringMapper() : base(typeof(String), new NVarcharAttribute(true))
        {
        }

        public override string MapToSql(object value)
        {
            var s = (string)value;
            return string.Format("N'{0}'", s.Replace("'", "''"));
        }
    }
}
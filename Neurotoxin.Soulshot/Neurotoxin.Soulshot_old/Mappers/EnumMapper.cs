using System;
using System.Linq;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Mappers
{
    public class EnumMapper : MapperBase
    {
        public EnumMapper() : base(typeof(Enum), new IntegerAttribute())
        {
        }

        public override object MapToType(object value, Type type)
        {
            if (!type.IsEnum)
            {
                if (type.IsGenericType)
                {
                    var arg = type.GenericTypeArguments.First();
                    if (arg.IsEnum && typeof(Nullable<>).MakeGenericType(arg) == type)
                    {
                        return Enum.ToObject(arg, value);
                    }
                }
                throw new NotSupportedException(type.Name + " is not enum");
            }
            return Enum.ToObject(type, value);
        }

        public override string MapToSql(object value)
        {
            var intValue = (int)value;
            return intValue.ToString();
        }
    }
}
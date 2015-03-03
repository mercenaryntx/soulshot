using System;
using System.Linq;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Mappers
{
    public class TypeMapper : MapperBase
    {
        public TypeMapper() : base(typeof(Type), new NVarcharAttribute(255))
        {
        }

        public override object MapFromSql(object value)
        {
            var stringValue = (string)value;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var typeValue = assembly.GetTypes().FirstOrDefault(t => t.FullName == stringValue);
                if (typeValue != null) return typeValue;
            }
            throw new Exception("Invalid type: " + stringValue);
        }

        public override object MapToSql(object value)
        {
            return ((Type)value).FullName;
        }
    }
}
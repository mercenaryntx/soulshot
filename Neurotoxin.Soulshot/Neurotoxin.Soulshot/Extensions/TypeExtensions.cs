using System;
using System.Text.RegularExpressions;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class TypeExtensions
    {
        public static int GetGenerationNumberFrom(this Type type, Type ancestor)
        {
            if (!ancestor.IsAssignableFrom(type))
                throw new ArgumentException(string.Format("Type {0} is not assignable from {1}", ancestor.FullName, type.FullName));

            var i = 0;
            var t = type;
            while (t != ancestor)
            {
                t = t.BaseType;
                i++;
            }
            return i;
        }

        public static TableAttribute GetTableAttribute(this Type type)
        {
            var attribute = type.GetAttribute<TableAttribute>();
            if (attribute != null) return attribute;
            var r = new Regex("Entity$");
            return new TableAttribute(r.Replace(type.Name, string.Empty));
        }
    }
}

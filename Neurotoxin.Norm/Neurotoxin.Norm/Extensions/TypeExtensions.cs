using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Extensions
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
            return type.GetAttribute<TableAttribute>() ?? new TableAttribute(type.Name);
        }
    }
}

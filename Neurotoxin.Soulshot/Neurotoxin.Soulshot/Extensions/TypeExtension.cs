using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;
using PetaPoco;

namespace Neurotoxin.Soulshot.Extensions
{
    public static class TypeExtension
    {
        private static readonly Dictionary<Type, ColumnMappingCollection> ColumnMappingCache = new Dictionary<Type, ColumnMappingCollection>();

        public static int GetGenerationNumberFrom(this Type type, Type ancestor)
        {
            if (!ancestor.IsAssignableFrom(type)) throw new ArgumentException($"Type {ancestor.FullName} is not assignable from {type.FullName}");

            var i = 0;
            var t = type;
            while (t != ancestor)
            {
                t = t.BaseType;
                i++;
            }
            return i;
        }

        public static ColumnMappingCollection GetColumnMappings(this Type baseType)
        {
            if (ColumnMappingCache.ContainsKey(baseType)) return ColumnMappingCache[baseType];

            var tableDefinition = baseType.GetCustomAttribute<TableAttribute>();
            var columns = new Dictionary<string, ColumnMapping>();
            //TODO: asm
            var mappedTypes = baseType.Assembly
                                      .GetTypes()
                                      .Where(baseType.IsAssignableFrom)
                                      .OrderBy(t => t.GetGenerationNumberFrom(baseType))
                                      .ThenBy(t => t.Name)
                                      .ToArray();
            var index = 0;

            if (mappedTypes.Length > 1)
                columns.Add(ColumnMapping.DiscriminatorColumnName, new ColumnMapping
                {
                    Column = new ColumnAttribute(ColumnMapping.DiscriminatorColumnName),
                    //ColumnType = "nvarchar(255)",
                    IsDiscriminatorColumn = true,
                    Index = index++
                });

            foreach (var pi in mappedTypes.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            {
                if (IsIgnorable(pi)) continue;
                var cm = new ColumnMapping
                {
                    Property = pi,
                    Column = pi.GetCustomAttribute<ColumnAttribute>(),
                    DeclaringTypes = new List<Type> { pi.DeclaringType },
                    Index = index
                };
                if (columns.ContainsKey(cm.ColumnName))
                {
                    //if (columns[columnName].ColumnType != columnType)
                    //{
                    //    columnName = pi.DeclaringType.Name + pi.Name;
                    //}
                    //else
                    //{
                    columns[cm.ColumnName].DeclaringTypes.Add(pi.DeclaringType);
                    continue;
                    //}
                }

                index++;
                columns.Add(cm.ColumnName, cm);
            }
            var mapping = new ColumnMappingCollection(columns.Values, tableDefinition);
            ColumnMappingCache.Add(baseType, mapping);
            return mapping;
        }

        private static bool IsIgnorable(PropertyInfo pi)
        {
            return pi.GetCustomAttribute<IgnoreAttribute>() != null;
        }
    }
}
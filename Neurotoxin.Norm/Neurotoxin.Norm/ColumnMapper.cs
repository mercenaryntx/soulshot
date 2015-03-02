using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;

namespace Neurotoxin.Norm
{
    public static class ColumnMapper
    {
        private readonly static Dictionary<Type, ColumnTypeAttribute> DefaultColumnTypes = new Dictionary<Type, ColumnTypeAttribute>
        {
            { typeof(Int16), new SmallIntAttribute() },
            { typeof(Int32), new IntegerAttribute() },
            { typeof(Int64), new BigIntAttribute() },
            { typeof(UInt16), new SmallIntAttribute() },
            { typeof(UInt32), new IntegerAttribute() },
            { typeof(UInt64), new BigIntAttribute() },
            { typeof(Decimal), new DecimalAttribute() },
            { typeof(Single), new FloatAttribute() },
            { typeof(Double), new FloatAttribute() },
            { typeof(String), new NVarcharAttribute(true) },
            { typeof(Char), new CharAttribute(1) },
            { typeof(DateTime), new DateTime2Attribute() },
            { typeof(DateTimeOffset), new DateTimeOffsetAttribute() },
            { typeof(TimeSpan), new TimeAttribute() },
            { typeof(Boolean), new BooleanAttribute() },
            { typeof(byte[]), new VarbinaryAttribute() },
            { typeof(Guid), new UniqueIdentifierAttribute() },
            { typeof(Enum), new IntegerAttribute() },
            { typeof(Type), new NVarcharAttribute(255) }
        };


        public static List<ColumnInfo> Map<TEntity>(TableAttribute table)
        {
            var columns = new Dictionary<string, ColumnInfo>();
            var baseType = typeof(TEntity);
            var types = baseType.Assembly.GetTypes()
                                         .Where(baseType.IsAssignableFrom)
                                         .OrderBy(t => t.GetGenerationNumberFrom(baseType))
                                         .ThenBy(t => t.Name)
                                         .ToList();
            foreach (var pi in types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            {
                if (IsIgnorable(pi)) continue;
                var columnType = GetColumnType(pi);
                var columnName = pi.Name;
                if (columns.ContainsKey(columnName) && columns[columnName].ColumnType != columnType)
                {
                    columnName = pi.DeclaringType.Name + pi.Name;
                }
                columns.Add(columnName, new ColumnInfo
                {
                    TableName = table.Name,
                    TableSchema = table.Schema,
                    ColumnName = columnName,
                    ColumnType = columnType,
                    BaseType = pi.DeclaringType,
                    PropertyName = pi.Name,
                    IsNullable = !pi.PropertyType.IsValueType,
                    IsIdentity = pi.HasAttribute<KeyAttribute>()
                });
            }
            return columns.Values.ToList();
        }

        private static bool IsIgnorable(PropertyInfo pi)
        {
            return pi.HasAttribute<IgnoreAttribute>();
        }

        private static string GetColumnType(PropertyInfo pi)
        {
            var attribute = pi.GetAttribute<ColumnTypeAttribute>() ?? GetDefaultColumnType(pi.PropertyType);
            return attribute.ToString();
        }

        private static ColumnTypeAttribute GetDefaultColumnType(Type type)
        {
            if (type.IsEnum) type = typeof(Enum);
            if (DefaultColumnTypes.ContainsKey(type)) return DefaultColumnTypes[type];
            throw new NotSupportedException("Unmappable type: " + type.FullName);
        }

    }
}

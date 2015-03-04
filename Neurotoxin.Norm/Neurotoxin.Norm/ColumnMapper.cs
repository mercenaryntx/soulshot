using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Extensions;
using Neurotoxin.Norm.Mappers;

namespace Neurotoxin.Norm
{
    public static class ColumnMapper
    {
        public const string DiscriminatorColumnName = "Discriminator";
        public static readonly Dictionary<Type, ColumnTypeAttribute> DefaultTypeAttributes = new Dictionary<Type, ColumnTypeAttribute>();
        public static readonly Dictionary<KeyValuePair<Type, ColumnTypeAttribute>, MapperBase> Mappers = new Dictionary<KeyValuePair<Type, ColumnTypeAttribute>, MapperBase>();
        public static readonly Dictionary<Type, List<ColumnInfo>> Cache = new Dictionary<Type, List<ColumnInfo>>();

        static ColumnMapper()
        {
            var mapperbase = typeof (MapperBase);
            foreach (var type in mapperbase.Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && mapperbase.IsAssignableFrom(t)))
            {
                var mapper = (MapperBase)Activator.CreateInstance(type);
                DefaultTypeAttributes.Add(mapper.PropertyType, mapper.ColumnType);
                Mappers.Add(new KeyValuePair<Type, ColumnTypeAttribute>(mapper.PropertyType, mapper.ColumnType), mapper);
            }
        }

        public static List<ColumnInfo> Map<TEntity>(TableAttribute table)
        {
            var columns = new Dictionary<string, ColumnInfo>();
            var baseType = typeof(TEntity);
            var types = baseType.Assembly.GetTypes()
                                         .Where(baseType.IsAssignableFrom)
                                         .OrderBy(t => t.GetGenerationNumberFrom(baseType))
                                         .ThenBy(t => t.Name)
                                         .ToList();

            if (types.Count > 1)
                columns.Add(DiscriminatorColumnName, new ColumnInfo
                {
                    TableName = table.Name,
                    TableSchema = table.Schema,
                    ColumnName = DiscriminatorColumnName,
                    ColumnType = DefaultTypeAttributes[typeof(string)].ToString(),
                    IsDiscriminatorColumn = true
                });

            foreach (var pi in types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            {
                if (IsIgnorable(pi)) continue;
                var columnType = GetColumnType(pi).ToString();
                var columnName = pi.Name;
                if (columns.ContainsKey(columnName))
                {
                    if (columns[columnName].ColumnType != columnType)
                    {
                        columnName = pi.DeclaringType.Name + pi.Name;
                    }
                    else
                    {
                        columns[columnName].DeclaringTypes.Add(pi.DeclaringType);
                        continue;
                    }
                }

                //TODO: proper nullable check
                var isNullable = pi.PropertyType.IsClass;
                object defaultValue = isNullable ? null : Activator.CreateInstance(pi.PropertyType);
                columns.Add(columnName, new ColumnInfo
                {
                    TableName = table.Name,
                    TableSchema = table.Schema,
                    ColumnName = columnName,
                    ColumnType = columnType,
                    DeclaringTypes = new List<Type> { pi.DeclaringType },
                    PropertyName = pi.Name,
                    IsNullable = isNullable,
                    IsIdentity = pi.HasAttribute<KeyAttribute>(),
                    DefaultValue = defaultValue
                });
            }

            var list = columns.Values.ToList();
            Cache[baseType] = list;
            return list;
        }

        public static object MapToType(object value, PropertyInfo pi = null)
        {
            var type = pi != null ? pi.PropertyType : value.GetType();
            var columnType = pi != null ? GetColumnType(pi) : GetDefaultColumnType(type);
            var mapper = GetMapper(type, columnType);
            return mapper.MapToType(value);
        }

        public static string MapToSql(object value)
        {
            if (value == null) return "null";
            var type = value.GetType();
            var columnType = GetDefaultColumnType(type);
            var mapper = GetMapper(type, columnType);
            return mapper.MapToSql(value);
        }

        public static Type MapType(object value)
        {
            var columnType = GetDefaultColumnType(typeof(string));
            var mapper = GetMapper(typeof(Type), columnType);
            return (Type)mapper.MapToType(value);
        }

        internal static MapperBase GetMapper(Type propertyType, ColumnTypeAttribute columnType = null)
        {
            var mapper = TryGetMapper(propertyType, columnType);
            if (mapper == null)
            {
                if (columnType == null) columnType = GetDefaultColumnType(propertyType);
                throw new Exception(string.Format("Mapper not found: {0} <-> {1}", propertyType, columnType));
            }
            return mapper;
        }

        internal static MapperBase TryGetMapper(Type propertyType, ColumnTypeAttribute columnType = null)
        {
            if (columnType == null)
            {
                if (propertyType.IsEnum) propertyType = typeof(Enum);
                if (!DefaultTypeAttributes.ContainsKey(propertyType)) return null;
                columnType = DefaultTypeAttributes[propertyType];
            }
            var key = new KeyValuePair<Type, ColumnTypeAttribute>(propertyType, columnType);
            return Mappers.ContainsKey(key) ? Mappers[key] : null;
        }

        private static bool IsIgnorable(PropertyInfo pi)
        {
            return pi.HasAttribute<IgnoreAttribute>();
        }

        internal static ColumnTypeAttribute GetColumnType(PropertyInfo pi)
        {
            var attribute = pi.GetAttribute<ColumnTypeAttribute>() ?? GetDefaultColumnType(pi.PropertyType);
            return attribute;
        }

        private static ColumnTypeAttribute GetDefaultColumnType(Type type)
        {
            if (type.IsEnum) type = typeof(Enum);
            if (DefaultTypeAttributes.ContainsKey(type)) return DefaultTypeAttributes[type];
            throw new NotSupportedException("Unmappable type: " + type.FullName);
        }

    }
}
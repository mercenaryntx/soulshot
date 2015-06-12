using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Extensions;
using Neurotoxin.Soulshot.Mappers;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot
{
    public class ColumnMapper
    {
        public const string DiscriminatorColumnName = "Discriminator";

        private readonly Dictionary<Type, ColumnTypeAttribute> _defaultTypeAttributes = new Dictionary<Type, ColumnTypeAttribute>();
        private readonly Dictionary<KeyValuePair<Type, ColumnTypeAttribute>, MapperBase> _mappers = new Dictionary<KeyValuePair<Type, ColumnTypeAttribute>, MapperBase>();
        private readonly Dictionary<Type, ColumnInfoCollection> _typeCache = new Dictionary<Type, ColumnInfoCollection>();
        public DbContext Context { get; internal set; }

        public ColumnMapper()
        {
            var mapperbase = typeof (MapperBase);
            foreach (
                var type in
                    mapperbase.Assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && mapperbase.IsAssignableFrom(t) && !t.IsGenericType))
            {
                var mapper = (MapperBase) Activator.CreateInstance(type);
                _defaultTypeAttributes.Add(mapper.PropertyType, mapper.ColumnType);
                _mappers.Add(new KeyValuePair<Type, ColumnTypeAttribute>(mapper.PropertyType, mapper.ColumnType), mapper);
            }
        }

        public ColumnInfoCollection this[Type key]
        {
            get { return _typeCache[key]; }
        }

        public bool ContainsKey(Type key)
        {
            return _typeCache.ContainsKey(key);
        }

        public ColumnInfoCollection Map<TEntity>(TableAttribute table = null)
        {
            return Map(typeof(TEntity), table);
        }

        public ColumnInfoCollection Map(Type baseType, TableAttribute table = null)
        {
            if (ContainsKey(baseType)) return this[baseType];

            var collection = new ColumnInfoCollection(baseType, table);
            var columns = new Dictionary<string, ColumnInfo>();

            var types = baseType.Assembly.GetTypes()
                                         .Where(baseType.IsAssignableFrom)
                                         .OrderBy(t => t.GetGenerationNumberFrom(baseType))
                                         .ThenBy(t => t.Name)
                                         .ToList();

            if (types.Count > 1)
                columns.Add(DiscriminatorColumnName, new ColumnInfo
                {
                    TableName = table != null ? table.Name : null,
                    TableSchema = table != null ? table.Schema : null,
                    ColumnName = DiscriminatorColumnName,
                    ColumnType = "nvarchar(255)",
                    IsDiscriminatorColumn = true,
                    IndexType = IndexType.Default
                });

            foreach (var pi in types.SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)))
            {
                if (IsIgnorable(pi)) continue;
                var columnTypeAttribute = GetColumnType(pi);
                var crossTableReferenceAttribute = columnTypeAttribute as CrossTableReferenceAttribute;
                if (crossTableReferenceAttribute != null)
                {
                    collection.CrossReferences.Add(new CrossReference(crossTableReferenceAttribute.EntityType));
                    continue;
                }
                var foreignKeyAttribute = columnTypeAttribute as ForeignKeyAttribute;
                ColumnInfoCollection referenceTable = null;
                if (foreignKeyAttribute != null)
                {
                    referenceTable = foreignKeyAttribute.DbSet.Columns;
                }

                var columnType = columnTypeAttribute.ToString();
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

                var isNullable = IsNullable(pi);
                object defaultValue = null;
                try
                {
                    defaultValue = isNullable ? null : Activator.CreateInstance(pi.PropertyType);
                }
                catch { }

                var indexAttribute = pi.GetAttribute<IndexAttribute>();

                columns.Add(columnName, new ColumnInfo
                {
                    TableName = table != null ? table.Name : null,
                    TableSchema = table != null ? table.Schema : null,
                    ColumnName = columnName,
                    ColumnType = columnType,
                    DeclaringTypes = new List<Type> { pi.DeclaringType },
                    PropertyName = pi.Name,
                    IsNullable = isNullable,
                    IsIdentity = pi.HasAttribute<KeyAttribute>(),
                    DefaultValue = defaultValue,
                    IndexType = indexAttribute != null ? indexAttribute.Type : (IndexType?)null,
                    ReferenceTable = referenceTable
                });
            }

            collection.SetCollection(columns.Values);
            _typeCache[baseType] = collection;
            return collection;
        }

        public object MapToType(object value, PropertyInfo pi = null)
        {
            if (value is DBNull) return null;

            var type = pi != null ? pi.PropertyType : value.GetType();
            var columnType = pi != null ? GetColumnType(pi) : GetDefaultColumnType(type);
            var mapper = GetMapper(type, columnType);
            return mapper.MapToType(value, type);
        }

        public string MapToSql(object value)
        {
            if (value == null) return "null";

            var type = value.GetType();
            if (value is IEntityProxy) type = type.BaseType;
            var columnType = GetDefaultColumnType(type);
            var mapper = GetMapper(type, columnType);
            return mapper.MapToSql(value);
        }

        public Type MapType(object value)
        {
            var columnType = GetDefaultColumnType(typeof(string));
            var mapper = GetMapper(typeof(Type), columnType);
            return mapper.MapToType<Type>(value);
        }

        internal MapperBase GetMapper(Type propertyType, ColumnTypeAttribute columnType = null)
        {
            var mapper = TryGetMapper(propertyType, columnType);
            if (mapper == null)
            {
                if (columnType == null) columnType = GetDefaultColumnType(propertyType);
                throw new Exception(string.Format("Mapper not found: {0} <-> {1}", propertyType, columnType));
            }
            return mapper;
        }

        internal MapperBase TryGetMapper(Type propertyType, ColumnTypeAttribute columnType = null)
        {
            if (IsEnum(propertyType)) propertyType = typeof(Enum);
            if (columnType == null)
            {
                if (!_defaultTypeAttributes.ContainsKey(propertyType)) return null;
                columnType = _defaultTypeAttributes[propertyType];
            }
            var key = new KeyValuePair<Type, ColumnTypeAttribute>(propertyType, columnType);
            return _mappers.ContainsKey(key) ? _mappers[key] : null;
        }

        private bool IsIgnorable(PropertyInfo pi)
        {
            return pi.HasAttribute<IgnoreAttribute>();
        }

        internal ColumnTypeAttribute GetColumnType(PropertyInfo pi)
        {
            var attribute = pi.GetAttribute<ColumnTypeAttribute>() ?? GetDefaultColumnType(pi.PropertyType);
            return attribute;
        }

        private ColumnTypeAttribute GetDefaultColumnType(Type type)
        {
            if (IsEnum(type)) type = typeof(Enum);
            if (_defaultTypeAttributes.ContainsKey(type)) return _defaultTypeAttributes[type];

            var foreignKey = IsEntityBasedProperty(type);
            if (foreignKey == null) throw new NotSupportedException("Unmappable type: " + type.FullName);

            if (type.IsGenericType)
            {
                _defaultTypeAttributes.Add(type, new CrossTableReferenceAttribute(type.GenericTypeArguments.First()));
            }
            else
            {
                var targetDbSet = Context.EnsureTable(type);
                var attribute = new ForeignKeyAttribute(GetColumnType(foreignKey).ToString(), targetDbSet);
                var mapperType = typeof (ForeignKeyMapper<>).MakeGenericType(type);
                var mapper = (MapperBase)Activator.CreateInstance(mapperType, attribute, targetDbSet);
                _mappers.Add(new KeyValuePair<Type, ColumnTypeAttribute>(type, attribute), mapper);
                _defaultTypeAttributes.Add(type, attribute);
            }
            return _defaultTypeAttributes[type];
        }

        private bool IsEnum(Type type)
        {
            if (type.IsEnum) return true;
            if (!type.IsGenericType) return false;
            var args = type.GenericTypeArguments;
            return args.All(t => t.IsEnum) && typeof(Nullable<>).MakeGenericType(args) == type;
        }

        private bool IsNullable(PropertyInfo pi)
        {
            if (pi.HasAttribute<KeyAttribute>() || pi.HasAttribute<IndexAttribute>()) return false;

            var type = pi.PropertyType;
            if (type.IsClass) return true;
            if (!type.IsGenericType) return false;
            var args = type.GenericTypeArguments;
            return args.All(t => t.IsValueType) && typeof (Nullable<>).MakeGenericType(args) == type;
        }

        private PropertyInfo IsEntityBasedProperty(Type type)
        {
            if (type.IsGenericType) type = type.GetGenericArguments().First();
            return type.IsClass ? type.GetProperties().SingleOrDefault(pi => pi.HasAttribute<KeyAttribute>()) : null;
        }

    }
}